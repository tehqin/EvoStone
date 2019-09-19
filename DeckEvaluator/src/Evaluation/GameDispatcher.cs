using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

using SabberStoneCoreAi.Score;

using SabberStoneUtil.Messaging;

namespace DeckEvaluator.Evaluation
{
   class GameDispatcher
   {
      // A list of all the games you are tasked with evaluating.
      private PlayerSetup _player;
      private List<PlayerSetup> _opponents;

      // Total stats for all the games played.
		private readonly object _statsLock = new object();
      private int _winCount;
      private Dictionary<string,int> _usageCount;
      private int _totalHealthDifference;
      private int _totalDamage;
      private int _totalTurns;
      private int _totalCardsDrawn;
      private int _totalHandSize;
      private int _totalManaSpent;
      private int _totalManaWasted;
      private int _totalStrategyAlignment;

		public GameDispatcher(PlayerSetup player, 
                            List<PlayerSetup> opponents)
		{
         // Save the configuration information.
         _player = player;
         _opponents = opponents;
      
         // Setup the statistics keeping.
         _usageCount = new Dictionary<string,int>();
         _winCount = 0;
         _totalDamage = 0;
         _totalHealthDifference = 0;
         _totalTurns = 0;
         _totalCardsDrawn = 0;
         _totalHandSize = 0;
         _totalManaSpent = 0;
         _totalManaWasted = 0;
         _totalStrategyAlignment = 0;
         foreach (Card curCard in _player.Deck.CardList)
         {
				if (!_usageCount.ContainsKey(curCard.Name))
				{
					_usageCount.Add(curCard.Name, 0);
				}
         }
      }

      private void runGame(int gameId, GameEvaluator ev)
      {
         Console.WriteLine("Starting game: "+gameId);

         // Run a game
         GameEvaluator.GameResult result = ev.PlayGame();
         
         // Record stats
         lock (_statsLock)
         {
            if (result._didWin)
            {
               _winCount++;
            }
            
	         foreach (string cardName in result._cardUsage.Keys)
            {
               if (_usageCount.ContainsKey(cardName))
               {
                  _usageCount[cardName] += result._cardUsage[cardName];
               }
            }
  
            _totalHealthDifference += result._healthDifference;
            _totalDamage += result._damageDone;
            _totalTurns += result._numTurns;
            _totalCardsDrawn += result._cardsDrawn;
            _totalHandSize += result._handSize;
            _totalManaSpent += result._manaSpent;
            _totalManaWasted += result._manaWasted;
            _totalStrategyAlignment += result._strategyAlignment;
         }

         Console.WriteLine("Finished game: "+gameId);
            
         // Rest every game.
         Thread.Sleep(1000);
      }

      private void queueGame(int gameId)
      {
      	var ev = new GameEvaluator(_player, _opponents[gameId]);
         runGame(gameId, ev);
      }

      private void WriteText(Stream fs, string s)
      {
         s += "\n";
         byte[] info = new UTF8Encoding(true).GetBytes(s);
         fs.Write(info, 0, info.Length);
      }

      public OverallStatistics Run()
      {
         Parallel.For(0, _opponents.Count, 
               new ParallelOptions {MaxDegreeOfParallelism = 8},
               i => {queueGame(i);});
         /*
         for (int i=0; i<_opponents.Count; i++)
            queueGame(i);
         */

         // Calculate turn averages from the totals
         double avgHealthDifference = _totalHealthDifference * 1.0 / _opponents.Count;
         double avgDamage = _totalDamage * 1.0 / _totalTurns;
         double avgCardsDrawn = _totalCardsDrawn * 1.0 / _totalTurns;
         double avgHandSize = _totalHandSize * 1.0 / _totalTurns;
         double avgManaSpent = _totalManaSpent * 1.0 / _totalTurns;
         double avgManaWasted = _totalManaWasted * 1.0 / _totalTurns;
         double avgStrategyAlignment = _totalStrategyAlignment * 1.0 / _totalTurns;
         double turnsPerGame = _totalTurns * 1.0 / _opponents.Count;

         // Pack up the results and give them back.
         var results = new OverallStatistics();
         string[] cardNames = _player.Deck.GetCardNames();
         results.UsageCounts = new int[cardNames.Length];
         for (int i=0; i<cardNames.Length; i++)
            results.UsageCounts[i] = _usageCount[cardNames[i]];
         results.WinCount = _winCount;
         results.AverageHealthDifference = avgHealthDifference;
         results.DamageDone = avgDamage;
         results.NumTurns = turnsPerGame;
         results.CardsDrawn = avgCardsDrawn;
         results.HandSize = avgHandSize;
         results.ManaSpent = avgManaSpent;
         results.ManaWasted = avgManaWasted;
         results.StrategyAlignment = avgStrategyAlignment;

         return results;
      }
   }
}
