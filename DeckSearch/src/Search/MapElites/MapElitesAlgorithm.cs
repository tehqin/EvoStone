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
      private MapElitesParams _params;
      private int _individualsEvaluated;
      private int _individualsDispatched;

      string[] featureNames;
      private FrequentMapLog _map_log;
      FeatureMap _featureMap;

      private const string ELITE_MAP_FILENAME = "logs/elite_map_log.csv";

      public MapElitesAlgorithm(MapElitesParams config)
      {
         _individualsDispatched = 0;
         _individualsEvaluated = 0;
         _params = config;

         InitMap();
      }

      private void InitMap()
      {
         var mapSizer = new LinearMapSizer(_params.Map.StartSize,
                                             _params.Map.EndSize);
         if (_params.Map.Type.Equals("SlidingFeature"))
               _featureMap = new SlidingFeatureMap(_params.Search.NumToEvaluate, _params.Map, mapSizer);
         else if (_params.Map.Type.Equals("FixedFeature"))
               _featureMap = new FixedFeatureMap(_params.Search.NumToEvaluate, _params.Map, mapSizer);
         else
               Console.WriteLine("ERROR: No feature map specified in config file.");

         featureNames = new string[_params.Map.Features.Length];
         for (int i = 0; i < _params.Map.Features.Length; i++)
               featureNames[i] = _params.Map.Features[i].Name;

         _map_log = new FrequentMapLog(ELITE_MAP_FILENAME, _featureMap);

      }

      public bool IsRunning() => _individualsEvaluated < _params.Search.NumToEvaluate;
      public bool IsBlocking() => _individualsDispatched >= _params.Search.InitialPopulation &&
                                 _individualsEvaluated == 0;
      public Individual GenerateIndividual(List<Card> cardSet)
      {
         _individualsDispatched++;
         return _individualsDispatched <= _params.Search.InitialPopulation ?
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
