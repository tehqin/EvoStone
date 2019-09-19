using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using Nett;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

using SabberStoneUtil.Decks;
using SabberStoneUtil.Messaging;

using DeckEvaluator.Config;
using DeckEvaluator.Evaluation;

namespace DeckEvaluator
{
   class Program
   {
      static void Main(string[] args)
      {
         string nodeName = args[0];
         int nodeId = Int32.Parse(args[0]);
         Console.WriteLine("Node Id: "+nodeId);
			
         // These files are for asyncronous communication between this
         // worker and it's scheduler. 
         //
         // Decks to evaluate come in the inbox and are dished out of the
         // outbox.
         string boxesDirectory = "boxes/";
         string inboxPath = boxesDirectory + 
            string.Format("deck-{0,4:D4}-inbox.tml", nodeId);
         string outboxPath = boxesDirectory +
            string.Format("deck-{0,4:D4}-outbox.tml", nodeId);
			
         // Hailing
         string activeDirectory = "active/";
         string activeWorkerPath = activeDirectory + 
            string.Format("worker-{0,4:D4}.txt", nodeId);
         string activeSearchPath = activeDirectory + "search.txt";
         if (!File.Exists(activeSearchPath))
         {
            Console.WriteLine("No search has been found.");
            return;
         }

         // The opponent deck doesn't change so we can load it here.
         string[] textLines = File.ReadAllLines(activeSearchPath);
         Console.WriteLine("Config File: " + textLines[1]);
         var config = Toml.ReadFile<Configuration>(textLines[1]);

         // Apply nerfs if nerfs are available
         ApplyNerfs(config.Nerfs);
         
         // Setup the pools of card decks for possible opponents.
         var deckPoolManager = new DeckPoolManager();
         deckPoolManager.AddDeckPools(config.Evaluation.DeckPools);

         // Setup test suites: (strategy, deck) combos to play against.
         var suiteConfig = Toml.ReadFile<DeckSuite>(
               config.Evaluation.OpponentDeckSuite);
         var gameSuite = new GameSuite(suiteConfig.Opponents,
                                       deckPoolManager);

         // Let the scheduler know we are here.
			using (FileStream ow = File.Open(activeWorkerPath, 
                FileMode.Create, FileAccess.Write, FileShare.None))
			{
				WriteText(ow, "Hail!");
				ow.Close();
			}

         // Loop while the guiding search is running.
         while (File.Exists(activeSearchPath))
         {
            // Wait until we have some work.
            while (!File.Exists(inboxPath) && File.Exists(activeSearchPath))
            {
               Console.WriteLine("Waiting... ("+nodeId+")");
               Thread.Sleep(5000);
            }

            if (!File.Exists(activeSearchPath))
               break;
 
            // Wait for the file to be finish being written
            Thread.Sleep(5000);

            // Run games, evaluate the deck, and then save the results.
            var playMessage = Toml.ReadFile<PlayMatchesMessage>(inboxPath);
            Deck playerDeck = playMessage.Deck.ContructDeck();

            int numStrats = config.Evaluation.PlayerStrategies.Length;
				var stratStats = new StrategyStatistics[numStrats];
            var overallStats = new OverallStatistics();
            overallStats.UsageCounts = new int[playerDeck.CardList.Count];
            RecordDeckProperties(playerDeck, overallStats);
            for (int i=0; i<numStrats; i++)
            {
               // Setup the player with the current strategy
               PlayerStrategyParams curStrat = 
                  config.Evaluation.PlayerStrategies[i];
               var player = new PlayerSetup(playerDeck,
                  PlayerSetup.GetStrategy(curStrat.Strategy, 
                                          config.Network,
                                          playMessage.Strategy));
               
               List<PlayerSetup> opponents =
                  gameSuite.GetOpponents(curStrat.NumGames);

               var launcher = new GameDispatcher(
                        player, opponents
                     );
               
               // Run the game and collect statistics
               OverallStatistics stats = launcher.Run();
               stratStats[i] = new StrategyStatistics();
               stratStats[i].WinCount += stats.WinCount;
               stratStats[i].Alignment += stats.StrategyAlignment;
               overallStats.Accumulate(stats); 
            }

            // Write the results
            overallStats.ScaleByNumStrategies(numStrats);
            var results = new ResultsMessage();
            results.PlayerDeck = playMessage.Deck;
            results.OverallStats = overallStats;
            results.StrategyStats = stratStats;
            Toml.WriteFile<ResultsMessage>(results, outboxPath);
           
            // Wait for the TOML file to write (buffers are out of sync)
            // Then tell the search that we are done writing the file.
            Thread.Sleep(3000);
				File.Delete(inboxPath);
         
            // Cleanup.
            GC.Collect();

            // Look at all the files in the current directory.
            // Eliminate anythings that matches our log file.
            /*
            string[] oFiles = Directory.GetFiles(".", "DeckEvaluator.o*");
            foreach (string curFile in oFiles)
            {
               if (curFile.EndsWith(nodeName))
               {
                  File.Delete(curFile); 
               }
            }*/
         }
      }

		private static void WriteText(Stream fs, string s)
      {
         s += "\n";
         byte[] info = new UTF8Encoding(true).GetBytes(s);
         fs.Write(info, 0, info.Length);
      }

      private static void ApplyNerfs(NerfParams[] nerfs)
      {
         if (nerfs == null)
            return;

         foreach (var curNerf in nerfs)
            ApplyNerf(curNerf);
      }

      private static void ApplyNerf(NerfParams nerf)
      {
         Card cardToNerf = Cards.FromName(nerf.CardName);
         cardToNerf.Tags[GameTag.COST] = nerf.NewManaCost;
         cardToNerf.Tags[GameTag.ATK] = nerf.NewAttack;
         cardToNerf.Tags[GameTag.HEALTH] = nerf.NewHealth;

         string msg = string.Format("Nerfing ({0}) to ({1}, {2}/{3})",
               nerf.CardName, nerf.NewManaCost, 
               nerf.NewAttack, nerf.NewHealth);
         Console.WriteLine(msg);
      }

      private static void RecordDeckProperties(Deck deck,
										OverallStatistics stats)
      {
         // Calculate the dust cost of the deck
         int dust = 0;
         foreach (Card c in deck.CardList)
         {
            if (c.Rarity == Rarity.COMMON)
               dust += 40;
            else if (c.Rarity == Rarity.RARE)
               dust += 100;
            else if (c.Rarity == Rarity.EPIC)
               dust += 400;
            else if (c.Rarity == Rarity.LEGENDARY)
               dust += 1600;
         }

         // Calculate the sum of mana costs
         int deckManaSum = 0;
         foreach (Card c in deck.CardList)
            deckManaSum += c.Cost;

      	// Calculate the variance of mana costs
         double avgDeckMana =
            deckManaSum * 1.0 / deck.CardList.Count;
         double runningVariance = 0;
         foreach (Card c in deck.CardList)
         {
            double diff = c.Cost - avgDeckMana;
            runningVariance += diff * diff;
         }
         double deckManaVariance = runningVariance / deck.CardList.Count;

         // Calculate the number of minion and spell cards
         int numMinionCards = 0;
         int numSpellCards = 0;
         foreach (Card c in deck.CardList)
         {
            if (c.Type == CardType.MINION)
               numMinionCards++;
            else if (c.Type == CardType.SPELL)
               numSpellCards++;
         }

         // Record the properties
         stats.Dust = dust;
         stats.DeckManaSum = deckManaSum;
         stats.DeckManaVariance = deckManaVariance;
         stats.NumMinionCards = numMinionCards;
         stats.NumSpellCards = numSpellCards;
      }
   }
}
