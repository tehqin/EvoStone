/* Adapted from the C# implementation of pure_cma.py
 *    https://github.com/CMA-ES/pycma/blob/master/cma/purecma.py
 */

using System;
using System.Linq;
using System.Collections.Generic;

using StrategySearch.Config;
using StrategySearch.Mapping;
using StrategySearch.Search;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using LA = MathNet.Numerics.LinearAlgebra;

namespace StrategySearch.Emitters
{
   class OptimizingEmitter : Emitter
   {
      private int _numParams;
      private EmitterParams _params;

      private List<Individual> _population;
      private FeatureMap _featureMap;

		// CMA Parameters
		private double _mutationPower;
      private DecompMatrix _C;
      private LA.Vector<double> _pc, _ps;
      private LA.Vector<double> _mean;
      private int _individualsDispatched;
      private int _individualsEvaluated;
      private int _generation;

      public OptimizingEmitter(EmitterParams emParams, FeatureMap featureMap, int numParams)
      {
         _numParams = numParams;
         _params = emParams;

         NumReleased = 0;
         _generation = 0;
         _individualsDispatched = 0;
         _population = new List<Individual>();
         _featureMap = featureMap;
      
         if (_params.PopulationSize == -1)
            _params.PopulationSize = (int)(4.0+Math.Floor(3.0*Math.Log(_numParams)));
         if (_params.NumParents == -1)
            _params.NumParents = (int)(_params.PopulationSize / 2);
         
			reset();
      }

      private void reset()
      {
         if (_featureMap.EliteMap.Count == 0)
            _mean = LA.Vector<double>.Build.Dense(_numParams);	
         else
            _mean = DenseVector.OfArray(_featureMap.GetRandomElite().ParamVector);

         _mutationPower = _params.MutationPower;
         _pc = LA.Vector<double>.Build.Dense(_numParams);
         _ps = LA.Vector<double>.Build.Dense(_numParams);

         _C = new DecompMatrix(_numParams);

         _individualsEvaluated = 0;
      }

      private bool checkStop(List<Individual> parents)
      {
			if (_C.ConditionNumber > 1e14)
            return true;

         double area = _mutationPower * Math.Sqrt(_C.Eigenvalues.Maximum());
         if (area < 1e-11)
            return true;

         double flatness =
				Math.Abs(parents[0].Fitness - parents[parents.Count-1].Fitness);
         if (flatness < 1e-12)
            return true;

         return false;
      }

      public bool IsBlocking() => 
         _individualsDispatched > _params.PopulationSize * _params.OverflowFactor;

      public int NumReleased { get; set; }

      public Individual GenerateIndividual()
      {
			var randomVector = LA.Vector<double>
						.Build.Dense(_numParams, j => Sampler.gaussian());
         for (int i=0; i<_numParams; i++)
            randomVector[i] *= _mutationPower * Math.Sqrt(_C.Eigenvalues[i]);

         var p = _C.Eigenbasis * randomVector + _mean;

         var newIndividual = new Individual(_numParams);
         newIndividual.ParamVector = p.ToArray();
         newIndividual.Generation = _generation;
         _individualsDispatched++;
         NumReleased++;
         return newIndividual;
      }

      public void ReturnEvaluatedIndividual(Individual ind)
      {
         _featureMap.Add(ind);
         if (ind.Generation != _generation)
            return;
         
         _population.Add(ind);
         _individualsEvaluated++;

         if (_population.Count >= _params.PopulationSize)
         {
            var parents = _population.OrderByDescending(o => o.Fitness).Take(_params.NumParents).ToList();

            // Calculate fresh weights for the number of elites found
            var weights = LA.Vector<double>.Build.Dense(_params.NumParents);
            for (int i=0; i<_params.NumParents; i++)
               weights[i] = Math.Log(_params.NumParents+0.5)-Math.Log(i+1);
            weights /= weights.Sum();
            
            // Dynamically update the hyperparameters for CMA-ES
            double sumWeights = weights.Sum();
            double sumSquares = weights.Sum(x => x * x);
            double mueff = sumWeights * sumWeights / sumSquares;
            double cc = (4+mueff/_numParams) / (_numParams+4+2*mueff/_numParams); 
            double cs = (mueff+2) / (_numParams+mueff+5);
            double c1 = 2 / (Math.Pow(_numParams+1.3, 2) + mueff);
            double cmu = Math.Min(1-c1,
                     2*(mueff-2+1/mueff) / (Math.Pow(_numParams+2, 2)+mueff));
            double damps = 1+2*Math.Max(0, Math.Sqrt((mueff-1)/(_numParams+1))-1)+cs;
            double chiN = Math.Sqrt(_numParams) *
                     (1.0-1.0/(4.0*_numParams)+1.0/(21.0*Math.Pow(_numParams,2)));

            // Recombination of the new mean
            LA.Vector<double> oldMean = _mean;
            _mean = LA.Vector<double>.Build.Dense(_numParams);
            for (int i=0; i<_params.NumParents; i++)
               _mean += DenseVector.OfArray(parents[i].ParamVector) * weights[i];

            // Update the evolution path
            LA.Vector<double> y = _mean - oldMean;
            LA.Vector<double> z = _C.Invsqrt * y;
            _ps = (1.0-cs) * _ps + (Math.Sqrt(cs * (2.0 - cs) * mueff) / _mutationPower) * z;
            double left = _ps.DotProduct(_ps) / _numParams / 
                  (1.0-Math.Pow(1.0-cs, 2 * _individualsEvaluated / _params.PopulationSize));
            double right = 2.0 + 4.0 / (_numParams+1.0);
            double hsig = left < right ? 1 : 0;
            _pc = (1.0 - cc) * _pc + hsig * Math.Sqrt(cc * (2.0 - cc) * mueff) * y;

            // Covariance matrix update
            double c1a = c1 * (1.0 - (1.0 - hsig * hsig) * cc * (2.0 - cc));
            _C.C *= (1.0 - c1a - cmu);
            _C.C += c1 * _pc.OuterProduct(_pc);
            for (int i=0; i<_params.NumParents; i++)
            {
               LA.Vector<double> dv = DenseVector.OfArray(parents[i].ParamVector) - oldMean;
               _C.C += weights[i] * cmu * dv.OuterProduct(dv) / (_mutationPower * _mutationPower);
            }

            bool needsRestart = checkStop(parents);
            if (!needsRestart)
               _C.UpdateEigensystem();

            // Update sigma
            double cn = cs / damps;
            double sumSquarePs = _ps.DotProduct(_ps);
            _mutationPower *= Math.Exp(Math.Min(1, cn * (sumSquarePs / _numParams - 1) / 2));

            if (needsRestart)
               reset();

            _generation++;
            _individualsDispatched = 0;
            _population.Clear();
         }
      }
   }
}
