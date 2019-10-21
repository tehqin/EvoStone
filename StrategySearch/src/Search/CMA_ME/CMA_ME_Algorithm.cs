using System;
using System.Collections.Generic;

using StrategySearch.Config;
using StrategySearch.Emitters;
using StrategySearch.Logging;
using StrategySearch.Mapping;
using StrategySearch.Mapping.Sizers;

namespace StrategySearch.Search.CMA_ME
{
   class CMA_ME_Algorithm : SearchAlgorithm
   {
      public CMA_ME_Algorithm(CMA_ME_Params searchParams, int numParams)
      {
         _params = searchParams;
      
         _individualsEvaluated = 0;

         initMap();
         
         // Create the population of emitters.
         _emitters = new List<Emitter>();
         foreach (EmitterParams ep in searchParams.Emitters)
         {
            // Make this many emitters of the given type.
            for (int i=0; i<ep.Count; i++)
            {
               if (ep.Type.Equals("Improvement"))
                  _emitters.Add(new ImprovementEmitter(ep, _featureMap, numParams));
               else if (ep.Type.Equals("Optimizing"))
                  _emitters.Add(new OptimizingEmitter(ep, _featureMap, numParams));
               else if (ep.Type.Equals("RandomDirection"))
                  _emitters.Add(new RandomDirectionEmitter(ep, _featureMap, numParams));
               else
                  Console.WriteLine("Emitter Not Found: "+ep.Type);
            }
         }
      }

      private CMA_ME_Params _params;

      private List<Emitter> _emitters;
      private int _individualsEvaluated;
      private FeatureMap _featureMap;

      private FrequentMapLog _map_log;

      private void initMap()
      {
         var mapSizer = new LinearMapSizer(_params.Map.StartSize,
                                           _params.Map.EndSize);

         Console.WriteLine("Map Type: " + _params.Map.Type);
         if (_params.Map.Type.Equals("FixedFeature"))
            _featureMap = new FixedFeatureMap(_params.Search.NumToEvaluate,
                                              _params.Map, mapSizer);
         else if (_params.Map.Type.Equals("SlidingFeature"))
            _featureMap = new SlidingFeatureMap(_params.Search.NumToEvaluate,
                                                _params.Map, mapSizer);
         else
            Console.WriteLine("ERROR: No feature map specified in config file.");
      
         _map_log = new FrequentMapLog("logs/elite_map_log.csv", _featureMap);
      }

      public bool IsRunning() => _individualsEvaluated < _params.Search.NumToEvaluate;
      public bool IsBlocking()
      {
         foreach (Emitter em in _emitters)
            if (!em.IsBlocking())
               return false;
         return true;
      }


      public Individual GenerateIndividual()
      {
         for (int i=0; i<_emitters.Count; i++)
         {
            Console.WriteLine(string.Format("EM[{0}] = {1}", i, _emitters[i].NumReleased));
         }

         int pos = 0;
         Emitter em = null;
         for (int i=0; i<_emitters.Count; i++)
         {
            if (!_emitters[i].IsBlocking())
            {
               if (em == null || em.NumReleased > _emitters[i].NumReleased)
               {
                  em = _emitters[i];
                  pos = i;
               }
            }
         }

         if (em == null)
            return null;

         Individual ind = em.GenerateIndividual();
         ind.EmitterID = pos;
         Console.WriteLine("Emitting from: "+ind.EmitterID);
         return ind;
      }

      public void ReturnEvaluatedIndividual(Individual ind)
      {
         ind.ID = _individualsEvaluated;
         _individualsEvaluated++;

         ind.Features = new double[_params.Map.Features.Length];
         for (int i=0; i<_params.Map.Features.Length; i++)
            ind.Features[i] = ind.GetStatByName(_params.Map.Features[i].Name);

			_emitters[ind.EmitterID].ReturnEvaluatedIndividual(ind);

			Console.WriteLine("Map Coverage: "+_featureMap.EliteMap.Count);
         if (_individualsEvaluated % 100 == 0)
            _map_log.UpdateLog();
      }
   }
}
