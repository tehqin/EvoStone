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

using DeckSearch.Config;
using DeckSearch.Logging;
using DeckSearch.Search.MapElites;
using DeckSearch.Search.EvolutionStrategy;


namespace DeckSearch.Search
{
    class DistributedSearch
    {
        private Queue<int> _runningWorkers;
        private Queue<int> _idleWorkers;
        private Dictionary<int, Individual> _individualStable;

        private string _configFilename;

        // Search Algorithm
        private SearchAlgorithm _searchAlgo;

        // Deck Info
        readonly private CardClass _heroClass;
        readonly private List<Card> _cardSet;

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

        private const string _boxesDirectory = "boxes/";
        private const string _inboxTemplate = _boxesDirectory
               + "deck-{0,4:D4}-inbox.tml";
        private const string _outboxTemplate = _boxesDirectory
               + "deck-{0,4:D4}-outbox.tml";

        // Let the workers know we are here.
        private const string _activeWorkerTemplate = _activeDirectory
               + "worker-{0,4:D4}.txt";
        private const string _activeDirectory = "active/";

        private const string _activeSearchPath = _activeDirectory
               + "search.txt";

        public DistributedSearch(string configFilename)
        {
            // Grab the configuration info
            _configFilename = configFilename;
            var config = Toml.ReadFile<Configuration>(_configFilename);
            // Console.WriteLine("NumFeatures: " + config.Map.Features.Length);
            // foreach (var p in config.Map.Features)
            // {
            //     Console.WriteLine(p.Name);
            // }

            // Configuration for the search space
            _heroClass = CardReader.GetClassFromName(config.Deckspace.HeroClass);
            CardSet[] sets = CardReader.GetSetsFromNames(config.Deckspace.CardSets);
            _cardSet = CardReader.GetCards(_heroClass, sets);

            // Setup the logs to record the data on individuals
            InitLogs();

            // Set up search algorithm
            Console.WriteLine("Algo: " + config.Search.Type);
            if (config.Search.Type.Equals("MAP-Elites"))
            {
                var searchConfig = Toml.ReadFile<MapElitesParams>(config.Search.ConfigFilename);
                _searchAlgo = new MapElitesAlgorithm(searchConfig);
            }

            else if (config.Search.Type.Equals("EvolutionStrategy")) 
            {
                var searchConfig = Toml.ReadFile<EvolutionStrategyParams>(config.Search.ConfigFilename);
                _searchAlgo = new EvolutionStrategyAlgorithm(searchConfig);
            }
        }

        private void InitLogs()
        {
            _individualLog =
               new RunningIndividualLog(INDIVIDUAL_LOG_FILENAME);
            _championLog =
               new RunningIndividualLog(CHAMPION_LOG_FILENAME);
            _fittestLog =
               new RunningIndividualLog(FITTEST_LOG_FILENAME);
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
            deckParams.ClassName = _heroClass.ToString().ToLower();
            deckParams.CardList = cur.GetCards();

            var msg = new PlayMatchesMessage();
            msg.Deck = deckParams;

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
            var os = cur.OverallData;
            Console.WriteLine("------------------");
            Console.WriteLine(string.Format("Eval ({0}): {1}",
                              cur.ID,
                              string.Join("", cur.ToString())));
            Console.WriteLine("Win Count: " + os.WinCount);
            Console.WriteLine("Average Health Difference: "
                              + os.AverageHealthDifference);
            Console.WriteLine("Damage Done: " + os.DamageDone);
            Console.WriteLine("Num Turns: " + os.NumTurns);
            Console.WriteLine("Cards Drawn: " + os.CardsDrawn);
            Console.WriteLine("Hand Size: " + os.HandSize);
            Console.WriteLine("Mana Spent: " + os.ManaSpent);
            Console.WriteLine("Mana Wasted: " + os.ManaWasted);
            Console.WriteLine("Strategy Alignment: " + os.StrategyAlignment);
            Console.WriteLine("Dust: " + os.Dust);
            Console.WriteLine("Deck Mana Sum: " + os.DeckManaSum);
            Console.WriteLine("Deck Mana Variance: " + os.DeckManaVariance);
            Console.WriteLine("Num Minion Cards: " + os.NumMinionCards);
            Console.WriteLine("Num Spell Cards: " + os.NumSpellCards);
            Console.WriteLine("------------------");
            foreach (var fs in cur.StrategyData)
            {
                Console.WriteLine("WinCount: " + fs.WinCount);
                Console.WriteLine("Alignment: " + fs.Alignment);
                Console.WriteLine("------------------");
            }

            // Save stats
            bool didHitMaxWins =
               cur.OverallData.WinCount > _maxWins;
            bool didHitMaxFitness =
               cur.Fitness > _maxFitness;
            _maxWins = Math.Max(_maxWins, cur.OverallData.WinCount);
            _maxFitness = Math.Max(_maxFitness, cur.Fitness);

            // Log the individuals
            _individualLog.LogIndividual(cur);
            if (didHitMaxWins)
                _championLog.LogIndividual(cur);
            if (didHitMaxFitness)
                _fittestLog.LogIndividual(cur);
        }

        private void FindNewWorkers()
        {
            // Look for new workers.
            string[] hailingFiles = Directory.GetFiles(_activeDirectory);
            foreach (string activeFile in hailingFiles)
            {
                string prefix = _activeDirectory + "worker-";
                if (activeFile.StartsWith(prefix))
                {
                    string suffix = ".txt";
                    int start = prefix.Length;
                    int end = activeFile.Length - suffix.Length;
                    string label = activeFile.Substring(start, end - start);
                    int workerId = Int32.Parse(label);
                    _idleWorkers.Enqueue(workerId);
                    _individualStable.Add(workerId, null);
                    File.Delete(activeFile);
                    Console.WriteLine("Found worker " + workerId);
                }
            }
        }

        public void Run()
        {
            // _individualsEvaluated = 0;
            _maxWins = 0;
            _maxFitness = Int32.MinValue;
            _runningWorkers = new Queue<int>();
            _idleWorkers = new Queue<int>();
            _individualStable = new Dictionary<int, Individual>();

            using (FileStream ow = File.Open(_activeSearchPath,
                     FileMode.Create, FileAccess.Write, FileShare.None))
            {
                WriteText(ow, "MAP Elites");
                WriteText(ow, _configFilename);
                ow.Close();
            }

            Console.WriteLine("Begin search...");
            while (_searchAlgo.IsRunning())
            {
                FindNewWorkers();

                // Dispatch jobs to the available workers.
                while (_idleWorkers.Count > 0 && !_searchAlgo.IsBlocking())
                {
                    int workerId = _idleWorkers.Dequeue();
                    _runningWorkers.Enqueue(workerId);
                    Console.WriteLine("Starting worker: " + workerId);

                    Individual choiceIndividual = _searchAlgo.GenerateIndividual(_cardSet);

                    string inboxPath = string.Format(_inboxTemplate, workerId);
                    SendWork(inboxPath, choiceIndividual);
                    _individualStable[workerId] = choiceIndividual;
                }

                // Look for individuals that are done.
                int numActiveWorkers = _runningWorkers.Count;
                for (int i = 0; i < numActiveWorkers; i++)
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
