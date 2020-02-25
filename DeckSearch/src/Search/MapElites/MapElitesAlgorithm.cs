using System;
using System.Collections.Generic;

using SabberStoneCore.Model;

using DeckSearch.Config;
using DeckSearch.Logging;
using DeckSearch.Mapping;
using DeckSearch.Mapping.Sizers;

namespace DeckSearch.Search.MapElites
{
   class MapElitesAlgorithm : SearchAlgorithm
   {
      private SearchParams _params;
      private int _individualsEvaluated;
      private int _individualsDispatched;

      string[] featureNames;
      private FrequentMapLog _map_log;
      FeatureMap _featureMap;

      private const string ELITE_MAP_FILENAME = "logs/elite_map_log.csv";

      public MapElitesAlgorithm(Configuration config)
      {
         _individualsDispatched = 0;
         _individualsEvaluated = 0;
         _params = config.Search;

         InitMap(config);
      }

      private void InitMap(Configuration config)
      {
         var mapSizer = new LinearMapSizer(config.Map.StartSize,
                                             config.Map.EndSize);
         if (config.Map.Type.Equals("SlidingFeature"))
               _featureMap = new SlidingFeatureMap(config, mapSizer);
         else if (config.Map.Type.Equals("FixedFeature"))
               _featureMap = new FixedFeatureMap(config, mapSizer);
         else
               Console.WriteLine("ERROR: No feature map specified in config file.");

         featureNames = new string[config.Map.Features.Length];
         for (int i = 0; i < config.Map.Features.Length; i++)
               featureNames[i] = config.Map.Features[i].Name;

         _map_log = new FrequentMapLog(ELITE_MAP_FILENAME, _featureMap);

      }

      public bool IsRunning() => _individualsEvaluated < _params.NumToEvaluate;
      public bool IsBlocking() => _individualsDispatched >= _params.InitialPopulation &&
                                 _individualsEvaluated == 0;
      public Individual GenerateIndividual(List<Card> cardSet)
      {
         _individualsDispatched++;
         return _individualsDispatched <= _params.InitialPopulation ?
                Individual.GenerateRandomIndividual(cardSet) :
                _featureMap.GetRandomElite().Mutate();
      }

      public void ReturnEvaluatedIndividual(Individual cur)
      {
         cur.ID = _individualsEvaluated;
         _individualsEvaluated++;

         cur.Features = new double[featureNames.Length];
         for (int i = 0; i < featureNames.Length; i++)
            cur.Features[i] = cur.GetStatByName(featureNames[i]);

         _featureMap.Add(cur);
         _map_log.UpdateLog();
      }
   }
}
