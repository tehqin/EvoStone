using System;
using System.Collections.Generic;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using LA = MathNet.Numerics.LinearAlgebra;

using StrategySearch.Config;
using StrategySearch.Logging;
using StrategySearch.Mapping;
using StrategySearch.Mapping.Sizers;

namespace StrategySearch.Search.MapElites
{
   class MapElitesLineAlgorithm : SearchAlgorithm
   {
      private static Random rnd = new Random();
      private static double gaussian(double stdDev)
      {
         double u1 = 1.0 - rnd.NextDouble();
         double u2 = 1.0 - rnd.NextDouble();
         double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1))
            * Math.Sin(2.0 * Math.PI * u2);
         return stdDev * randStdNormal;
      }

      private int _numParams;
      private MapElitesParams _params;

      private double _maxFitness;
      private int _individualsDispatched;
      private int _individualsEvaluated;
      private FeatureMap _featureMap;

      private FrequentMapLog _map_log;
      
      public MapElitesLineAlgorithm(MapElitesParams searchParams, int numParams)
      {
         _numParams = numParams;
         _params = searchParams;

         _individualsEvaluated = 0;
         _individualsDispatched = 0;
         _maxFitness = Double.MinValue;

         initMap();
      }

      private void initMap()
      {
         var mapSizer = new LinearMapSizer(_params.Map.StartSize,
                                           _params.Map.EndSize);
         
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
      public bool IsBlocking() => 
         _individualsDispatched == _params.Search.InitialPopulation &&
         _individualsEvaluated < _params.Search.InitialPopulation;

      public Individual GenerateIndividual()
      {
         _individualsDispatched += 1;
         if (_individualsDispatched <= _params.Search.InitialPopulation)
         {
            var ind = new Individual(_numParams);
            for (int i=0; i<_numParams; i++)
               ind.ParamVector[i] = rnd.NextDouble() * 2 - 1;
            return ind;
         }
         
         Individual parent1 = _featureMap.GetRandomElite();
         Individual parent2 = _featureMap.GetRandomElite();
         var child = new Individual(_numParams);

         // Perform the line mutation
         double scalar1 = _params.Search.MutationPower;
         double scalar2 = _params.Search.MutationPower2;
         var p1 = LA.Vector<double>.Build.DenseOfEnumerable(parent1.ParamVector);
         var p2 = LA.Vector<double>.Build.DenseOfEnumerable(parent2.ParamVector);
         var randomVector = MathNet.Numerics.LinearAlgebra.Vector<double>
            .Build.Dense(_numParams, j => gaussian(scalar1));
         var lineVector = scalar2 * (p2 - p1) * gaussian(1.0);

         p1 += randomVector + lineVector;

         for (int i=0; i<_numParams; i++)
            child.ParamVector[i] = p1[i];

         return child;
      }

      public void ReturnEvaluatedIndividual(Individual ind)
      {
         ind.ID = _individualsEvaluated;
         _individualsEvaluated++;
         
         ind.Features = new double[_params.Map.Features.Length];
         for (int i=0; i<_params.Map.Features.Length; i++)
            ind.Features[i] = ind.GetStatByName(_params.Map.Features[i].Name);

         _maxFitness = Math.Max(_maxFitness, ind.Fitness);
         _featureMap.Add(ind);

         if (_individualsEvaluated % 100 == 0)
            _map_log.UpdateLog();
      
         if (!IsRunning())
         {
            Console.WriteLine(string.Format("{0},{1},{2}", _params.Search.MutationPower, _maxFitness, _featureMap.EliteMap.Count));
         }
      }
   }
}
