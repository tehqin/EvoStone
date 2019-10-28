using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using LA = MathNet.Numerics.LinearAlgebra;

using StrategySearch.Config;
using StrategySearch.Emitters;

namespace StrategySearch.Search.CMA_ES
{
	class CMA_ES_Algorithm : SearchAlgorithm
	{
		private static Random rnd = new Random();
		private static double gaussian()
		{
			double u1 = 1.0 - rnd.NextDouble();
			double u2 = 1.0 - rnd.NextDouble();
			return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
		}

		private int _numParams;
		private CMA_ES_Params _params;

		private LA.Vector<double> _mean;
		private List<Individual> _population;
      private Individual _bestIndividual;
      private int _individualsDispatched;
		private int _individualsEvaluated;
		private int _individualsEvaluatedTotal;
		private int _generation;

      private MathNet.Numerics.LinearAlgebra.Vector<double> _weights;

      private double _mueff, _mutationPower, _cc, _cs, _c1, _cmu, _damps, _chiN;
      private LA.Vector<double> _pc, _ps;

      private DecompMatrix _C;

		public CMA_ES_Algorithm(CMA_ES_Params searchParams, int numParams)
		{
         _numParams = numParams;
			_params = searchParams;
			
			_population = new List<Individual>();
         _individualsDispatched = 0;
         _individualsEvaluatedTotal = 0;
         _generation = 0;
         _bestIndividual = null;

         Reset();
      }

      public void Reset()
      {
         if (_params.PopulationSize == -1)
            _params.PopulationSize = (int)(4.0+Math.Floor(3.0*Math.Log(_numParams)));
         if (_params.NumParents == -1)
            _params.NumParents = _params.PopulationSize / 2;
         _mutationPower = _params.MutationPower;

         _weights = MathNet.Numerics.LinearAlgebra.
            Vector<double>.Build.Dense(_params.NumParents);
         for (int i=0; i<_params.NumParents; i++)
            _weights[i] = Math.Log(_params.NumParents+0.5)-Math.Log(i+1);
         _weights /= _weights.Sum();
         double sum_weights = _weights.Sum();
         double sum_squares = _weights.Sum(x => x * x);
         _mueff = sum_weights * sum_weights / sum_squares;

         _mean = LA.Vector<double>.Build.Dense(_numParams);
         if (_bestIndividual != null)
         {
            for (int i=0; i<_numParams; i++)
               _mean[i] = _bestIndividual.ParamVector[i];
         }
         Console.WriteLine("RESET");
         Console.WriteLine(_mean);

         _cc = (4+_mueff/_numParams) / (_numParams+4 + 2*_mueff/_numParams);
         _cs = (_mueff+2) / (_numParams+_mueff+5);
         _c1 = 2 / (Math.Pow(_numParams+1.3, 2) + _mueff);
         _cmu = Math.Min(1-_c1, 
                2*(_mueff-2+1/_mueff) / (Math.Pow(_numParams+2, 2)+_mueff));
         _damps = 1+2*Math.Max(0, Math.Sqrt((_mueff-1)/(_numParams+1))-1)+_cs;
         _chiN = Math.Sqrt(_numParams) * 
            (1.0-1.0/(4.0*_numParams)+1.0/(21.0*Math.Pow(_numParams,2)));

         _pc = LA.Vector<double>.Build.Dense(_numParams);
         _ps = LA.Vector<double>.Build.Dense(_numParams);
		
         _C = new DecompMatrix(_numParams);
         
         _individualsEvaluated = 0;
      }

      public bool CheckStop(List<Individual> parents)
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

		public bool IsRunning() => _individualsEvaluatedTotal < _params.NumToEvaluate;
      public bool IsBlocking() => _individualsDispatched > _params.PopulationSize * _params.OverflowFactor;

		public Individual GenerateIndividual()
		{
			var randomVector = MathNet.Numerics.LinearAlgebra.Vector<double>
				.Build.Dense(_numParams, j => gaussian());
         for (int i=0; i<_numParams; i++)
            randomVector[i] *= _mutationPower * Math.Sqrt(_C.Eigenvalues[i]);

         var p = _C.Eigenbasis * randomVector + _mean;

			var newIndividual = new Individual(_numParams);
			newIndividual.ParamVector = p.ToArray();
         newIndividual.Generation = _generation;
         _individualsDispatched++;
			return newIndividual;
		}

		public void ReturnEvaluatedIndividual(Individual ind)
		{
         ind.ID = _individualsEvaluatedTotal;
			_individualsEvaluatedTotal++;

         if (_bestIndividual == null || _bestIndividual.Fitness < ind.Fitness)
            _bestIndividual = ind;

         if (ind.Generation != _generation)
            return;

         // Note that we don't use overflow individuals in adaptation calculations.
			_individualsEvaluated++;
			_population.Add(ind);
			if (_population.Count >= _params.PopulationSize)
			{
            // Rank solutions
				var parents = _population.OrderByDescending(o => o.Fitness)
					.Take(_params.NumParents).ToList();
            Console.WriteLine(parents[0].Fitness);

            // Recombination of the new mean
		      LA.Vector<double> oldMean = _mean;
            _mean = LA.Vector<double>.Build.Dense(_numParams);
            for (int i=0; i<_params.NumParents; i++)
               _mean += DenseVector.OfArray(parents[i].ParamVector) * _weights[i]; 

            // Update the evolution path
            LA.Vector<double> y = _mean - oldMean;
            LA.Vector<double> z = _C.Invsqrt * y;
            _ps = (1.0-_cs) * _ps + (Math.Sqrt(_cs * (2.0 - _cs) * _mueff) / _mutationPower) * z;
            double left = _ps.DotProduct(_ps) / _numParams
               / (1.0-Math.Pow(1.0-_cs, 2 * _individualsEvaluated / _params.PopulationSize));
            double right = 2.0 + 4.0 / (_numParams+1.0);
            double hsig = left < right ? 1 : 0;
            _pc = (1.0 - _cc) * _pc + hsig * Math.Sqrt(_cc * (2.0 - _cc) * _mueff) * y;

            // Covariance matrix update
            double c1a = _c1 * (1.0 - (1.0 - hsig * hsig) * _cc * (2.0 - _cc));
            _C.C *= (1.0 - c1a - _cmu);
            _C.C += _c1 * _pc.OuterProduct(_pc);
            for (int i=0; i<_params.NumParents; i++)
            {
               LA.Vector<double> dv = DenseVector.OfArray(parents[i].ParamVector) - oldMean;
               _C.C += _weights[i] * _cmu * dv.OuterProduct(dv) / (_mutationPower * _mutationPower);
            }

            if (CheckStop(parents))
               Reset();
            _C.UpdateEigensystem();

            // Update sigma
            double cn = _cs / _damps;
            double sumSquarePs = _ps.DotProduct(_ps);
            _mutationPower *= Math.Exp(Math.Min(1, cn * (sumSquarePs / _numParams - 1) / 2));

            _generation++;
            _individualsDispatched = 0;
            _population.Clear();
			}
		}
	}
}
