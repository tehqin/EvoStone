using System;
using System.Linq;
using System.Collections.Generic;

using StrategySearch.Config;

namespace StrategySearch.Search.EvolutionStrategy
{
   class EvolutionStrategyAlgorithm : SearchAlgorithm
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

      private static double clip(double v)
      {
         return Math.Max(Math.Min(v, 1.00), -1.00);
      }

      private int _numParams;
      private EvolutionStrategyParams _params;

      private List<Individual> _elites;
      private List<Individual> _population;
		private int _individualsEvaluated;

      public EvolutionStrategyAlgorithm(EvolutionStrategyParams searchParams, int numParams)
      {
         _numParams = numParams;
         _params = searchParams;

         _elites = null;
         _population = new List<Individual>();
         _individualsEvaluated = 0;
      }
 
		public bool IsRunning() => _individualsEvaluated < _params.NumToEvaluate;
		public bool IsBlocking() => false;
 
      public Individual GenerateIndividual()
      {
         if (_elites == null)
			{
				var ind = new Individual(_numParams);
				for (int i=0; i<_numParams; i++)
					ind.ParamVector[i] = rnd.NextDouble() * 2 - 1;
				return ind;
			}

         int pos = rnd.Next(_elites.Count);
			var child = new Individual(_numParams);
			double scalar = _params.MutationPower;
         for (int i=0; i<_numParams; i++)
            child.ParamVector[i] = clip(gaussian(scalar) + _elites[pos].ParamVector[i]);
         return child;
      }

      public void ReturnEvaluatedIndividual(Individual ind)
      {
         ind.ID = _individualsEvaluated;
         _individualsEvaluated++;
         _population.Add(ind);
         if (_population.Count >= _params.PopulationSize)
         {
            _elites = _population.OrderBy(o => o.Fitness)
               .Reverse().Take(_params.NumElites).ToList();
				_population.Clear(); 
         }
      }
   }
}
