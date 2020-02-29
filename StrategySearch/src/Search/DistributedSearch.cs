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

using StrategySearch.Config;
using StrategySearch.Logging;
using StrategySearch.Search.CMA_ES;
using StrategySearch.Search.CMA_ME;
using StrategySearch.Search.EvolutionStrategy;
using StrategySearch.Search.MapElites;

namespace StrategySearch.Search
{
   class DistributedSearch
   {
      private Queue<int> _runningWorkers;
      private Queue<int> _idleWorkers;
		private Dictionary<int,Individual> _individualStable;
      private Random rnd = new Random();

      private string _configFilename;
      
      // Search Algorithm
      private SearchAlgorithm _searchAlgo;

      // Deck info
      readonly private Deck _playerDeck; 

      // Logging 
      private const string LOG_DIRECTORY = "logs/";
      private const string INDIVIDUAL_LOG_FILENAME = 
         LOG_DIRECTORY + "individual_log.csv";
      private const string CHAMPION_LOG_FILENAME = 
         LOG_DIRECTORY + "champion_log.csv";
      private const string FITTEST_LOG_FILENAME = 
         LOG_DIRECTORY + "fittest_log.csv";
      private RunningIndividualLog _individualLog;
      private RunningIndividualLog _championLog;
      private RunningIndividualLog _fittestLog;

      // Node communication
      private const string _boxesDirectory = "boxes/";
      private const string _inboxTemplate = _boxesDirectory
         + "deck-{0,4:D4}-inbox.tml";
      private const string _outboxTemplate = _boxesDirectory
         + "deck-{0,4:D4}-outbox.tml";
      private const string _activeDirectory = "active/";
      private const string _activeWorkerTemplate = _activeDirectory
         + "worker-{0,4:D4}.txt";
      private const string _activeSearchPath = _activeDirectory
         + "search.txt";
         
      public DistributedSearch(string configFilename)
      {
         // Grab the config info
         _configFilename = configFilename;
         var config = Toml.ReadFile<Configuration>(_configFilename);

         // Figure out the number of search parameters based on whether
         // we are searching a linear combination or neural network weights
         int numParams = -1;
         if (config.Evaluation.PlayerStrategies[0].Strategy == "Custom")
            numParams = 15;
         else if (config.Evaluation.PlayerStrategies[0].Strategy == "NeuralNet")
         {
            // Peak in to the neural network configuration and compute
            // the number of parameters we need to compute weights for.
            int[] layerSizes = config.Network.LayerSizes;
            numParams = 0;
            for (int i=0; i<layerSizes.Length-1; i++)
            {
               numParams += layerSizes[i] * layerSizes[i+1]; // edge weights
               numParams += layerSizes[i+1]; // bias weights
            }
         }
         Console.WriteLine(string.Format("Search for {0} parameters...", numParams));

         // Setup the search algorithm to use to optimize the strategy.
         if (config.Search.Type.Equals("EvolutionStrategy"))
         {
            var searchConfig =
               Toml.ReadFile<EvolutionStrategyParams>(config.Search.ConfigFilename);
            _searchAlgo = new EvolutionStrategyAlgorithm(searchConfig, numParams);
         }
         else if (config.Search.Type.Equals("CMA-ES"))
         {
            var searchConfig = 
               Toml.ReadFile<CMA_ES_Params>(config.Search.ConfigFilename);
            _searchAlgo = new CMA_ES_Algorithm(searchConfig, numParams);
			}
         else if (config.Search.Type.Equals("CMA-ME"))
         {
            var searchConfig = 
               Toml.ReadFile<CMA_ME_Params>(config.Search.ConfigFilename);
            _searchAlgo = new CMA_ME_Algorithm(searchConfig, numParams);
         }
         else if (config.Search.Type.Equals("MAP-Elites"))
         {
            var searchConfig = 
               Toml.ReadFile<MapElitesParams>(config.Search.ConfigFilename);
            _searchAlgo = new MapElitesAlgorithm(searchConfig, numParams);
         }
         else
         {
            Console.WriteLine(string.Format("Strategy {} not supported.", 
                     config.Search.Type));
         }

         // Setup the deck pool and grab our deck and class
         var deckPoolManager = new DeckPoolManager();
         deckPoolManager.AddDeckPools(config.Evaluation.DeckPools);
         string poolName = config.Player.DeckPool;
         string deckName = config.Player.DeckName;
         Console.WriteLine(string.Format("names {0} {1}", poolName, deckName));
         _playerDeck = deckPoolManager.GetDeck(poolName, deckName);

         // Setup the logs to record the data on individuals
         InitLogs();
      }

      private void InitLogs()
      {
         _individualLog = new RunningIndividualLog(INDIVIDUAL_LOG_FILENAME);
         _championLog = new RunningIndividualLog(CHAMPION_LOG_FILENAME);
         _fittestLog = new RunningIndividualLog(FITTEST_LOG_FILENAME);
      }
      
      private static void WriteText(Stream fs, string s)
      {
         s += "\n";
         byte[] info = new UTF8Encoding(true).GetBytes(s);
         fs.Write(info, 0, info.Length);
      }

      private void SendWork(string workerInboxPath, Individual cur)
      {
         var deckParams = new DeckParams();
         deckParams.ClassName = _playerDeck.DeckClass.ToString();
         deckParams.CardList = _playerDeck.GetCardNames();
         CustomStratWeights weights = cur.GetWeights(); 

         var msg = new PlayMatchesMessage();
         msg.Deck = deckParams;
         msg.Strategy = weights;

         Toml.WriteFile<PlayMatchesMessage>(msg, workerInboxPath);
      }

      private double _maxWins;
      private double _maxFitness;
      private void ReceiveResults(string workerOutboxPath, Individual cur)
      {
         // Read the message and then delete the file.
         var results = Toml.ReadFile<ResultsMessage>(workerOutboxPath);
         File.Delete(workerOutboxPath);

			// Save the statistics for this individual.
         cur.OverallData = results.OverallStats;
         cur.StrategyData = results.StrategyStats;
 
         // Save which elements are relevant to the search
         cur.Fitness = cur.OverallData.AverageHealthDifference;
      }

      private void LogIndividual(Individual cur)
      {
         // Standard IO Logging
         var os = cur.OverallData;
         Console.WriteLine("------------------");
         Console.WriteLine(string.Format("Eval ({0}):", cur.ID));
         Console.WriteLine("Win Count: "+os.WinCount);
         Console.WriteLine("Average Health Difference: "
                           +os.AverageHealthDifference);
         Console.WriteLine("Damage Done: "+os.DamageDone);
         Console.WriteLine("Num Turns: "+os.NumTurns);
         Console.WriteLine("Cards Drawn: "+os.CardsDrawn);
         Console.WriteLine("Hand Size: "+os.HandSize);
         Console.WriteLine("Mana Spent: "+os.ManaSpent);
         Console.WriteLine("Mana Wasted: "+os.ManaWasted);
         Console.WriteLine("Strategy Alignment: "+os.StrategyAlignment);
         Console.WriteLine("Dust: "+os.Dust);
         Console.WriteLine("Deck Mana Sum: "+os.DeckManaSum);
         Console.WriteLine("Deck Mana Variance: "+os.DeckManaVariance);
         Console.WriteLine("Num Minion Cards: "+os.NumMinionCards);
         Console.WriteLine("Num Spell Cards: "+os.NumSpellCards);
         Console.WriteLine("------------------");
         foreach (var fs in cur.StrategyData)
         {
            Console.WriteLine("WinCount: "+fs.WinCount);
            Console.WriteLine("Alignment: "+fs.Alignment);
            Console.WriteLine("------------------");
         }

         // Save stats
         bool didHitMaxWins = cur.OverallData.WinCount > _maxWins;
         bool didHitMaxFitness = cur.Fitness > _maxFitness;
         _maxWins = Math.Max(_maxWins, cur.OverallData.WinCount);
         _maxFitness = Math.Max(_maxFitness, cur.Fitness);
         
         // Log the individuals
         _individualLog.LogIndividual(cur);
         if (cur.OverallData.WinCount == _maxWins)
            _championLog.LogIndividual(cur);
         if (cur.Fitness == _maxFitness)
            _fittestLog.LogIndividual(cur);
      }

      private void FindNewWorkers()
      {
         string[] hailingFiles = Directory.GetFiles(_activeDirectory);
         foreach (string activeFile in hailingFiles)
         {
            string prefix = _activeDirectory + "worker-";
   			if (activeFile.StartsWith(prefix))
				{
					string suffix = ".txt";
					int start = prefix.Length;
					int end = activeFile.Length - suffix.Length;
					string label = activeFile.Substring(start, end-start);
					int workerId = Int32.Parse(label);
					_idleWorkers.Enqueue(workerId);
					_individualStable.Add(workerId, null);
					File.Delete(activeFile);
					Console.WriteLine("Found worker: " + workerId);
				}
         }
      }

      public void Run()
      {
         _maxWins = 0;
         _maxFitness = Int32.MinValue;
         _runningWorkers = new Queue<int>();
         _idleWorkers = new Queue<int>();
         _individualStable = new Dictionary<int,Individual>();
         
         // Let the workers know we are here.
			using (FileStream ow = File.Open(_activeSearchPath,
						FileMode.Create, FileAccess.Write, FileShare.None))
			{
				WriteText(ow, "Strategy Search");
				WriteText(ow, _configFilename);
				ow.Close();
			}

         Console.WriteLine("Begin search...");
         while (_searchAlgo.IsRunning())
         {
				FindNewWorkers();
            
            // Disbatch jobs to the available workers.
            while (_idleWorkers.Count > 0 && !_searchAlgo.IsBlocking())
            {
               int workerId = _idleWorkers.Dequeue();
               _runningWorkers.Enqueue(workerId);
               Console.WriteLine("Starting worker: "+workerId);

               Individual choiceIndividual = _searchAlgo.GenerateIndividual();

               string inboxPath = string.Format(_inboxTemplate, workerId);
               Console.WriteLine(inboxPath);
               Console.WriteLine(choiceIndividual.ToString());
               SendWork(inboxPath, choiceIndividual);
               _individualStable[workerId] = choiceIndividual;
            }

            // Look for individuals that are done.
            int numActiveWorkers = _runningWorkers.Count;
            for (int i=0; i<numActiveWorkers; i++)
            {
               int workerId = _runningWorkers.Dequeue();
               string inboxPath = string.Format(_inboxTemplate, workerId);
               string outboxPath = string.Format(_outboxTemplate, workerId);

               // Test if this worker is done.
               if (File.Exists(outboxPath) && !File.Exists(inboxPath))
               {
                  // Wait for the file to finish being written.
                  Console.WriteLine("Worker done: " + workerId);

                  ReceiveResults(outboxPath, _individualStable[workerId]);
                  _searchAlgo.ReturnEvaluatedIndividual(_individualStable[workerId]);
                  LogIndividual(_individualStable[workerId]);
                  _idleWorkers.Enqueue(workerId);
               }
               else
               {
                  _runningWorkers.Enqueue(workerId);
               }
            }

            Thread.Sleep(1000);
         }

         // Let the workers know that we are done.
         File.Delete(_activeSearchPath);
      }
   }
}
