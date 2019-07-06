using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;

using StrategySearch.Config;

namespace StrategySearch.Search.CMA_ES
{
	class CMA_ES_Algorithm : SearchAlgorithm
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
		private CMA_ES_Params _params;

		private MathNet.Numerics.LinearAlgebra.Vector<double> _mean;
		private List<Individual> _population;
		private int _individualsEvaluated;

		Matrix<double> _covarianceMatrix;
		Matrix<double> _distributionTransform;

		public CMA_ES_Algorithm(CMA_ES_Params searchParams, int numParams)
		{
         _numParams = numParams;
			_params = searchParams;

			_population = new List<Individual>();
			_individualsEvaluated = 0;
			_mean = MathNet.Numerics.LinearAlgebra.Vector<double>
							.Build.Dense(_numParams);
			_covarianceMatrix = DenseMatrix.CreateIdentity(_numParams);
			_distributionTransform = _covarianceMatrix;
		}

		public bool IsRunning() => _individualsEvaluated < _params.NumToEvaluate;

		public Individual GenerateIndividual()
		{
			var randomVector = MathNet.Numerics.LinearAlgebra.Vector<double>
				.Build.Dense(_numParams, j => gaussian(_params.MutationScalar));
			var p = _distributionTransform * randomVector + _mean;
			var newIndividual = new Individual(_numParams);
			newIndividual.ParamVector = p.ToArray();
			for (int i=0; i<_numParams; i++)
            newIndividual.ParamVector[i] = newIndividual.ParamVector[i];
			return newIndividual;
		}

		public void ReturnEvaluatedIndividual(Individual ind)
		{
         ind.ID = _individualsEvaluated;
			_individualsEvaluated++;
			_population.Add(ind);
			if (_population.Count >= _params.PopulationSize)
			{
				// Compute the rotation and stretching transformation
				Evd<double> evd = _covarianceMatrix.Evd();
				var values = DiagonalMatrix.OfDiagonal(_numParams, _numParams,
						evd.EigenValues.Select(c => Complex.Sqrt(c).Real));
				Matrix<double> vectors = evd.EigenVectors;
				_distributionTransform = vectors * values;

				// Grab the elites
				var elites = _population.OrderByDescending(o => o.Fitness)
					.Take(_params.NumElites).ToList();

				// Update the covariance matrix
				for (int i=0; i<_numParams; i++)
				{
					for (int j=0; j<_numParams; j++)
					{
						double cell = 0;
						for (int k=0; k<elites.Count; k++)
						{
							double left = elites[k].ParamVector[i] - _mean[i];
							double right = elites[k].ParamVector[j] - _mean[j];
							cell += left * right;
						}
						cell /= elites.Count;
						_covarianceMatrix[i,j] = 0.8 * _covarianceMatrix[i,j]
														+ 0.2 * cell;
					}
				}

				// Calculate the new mean
				var sums = new double[_numParams];
				foreach (Individual cur in elites)
					for (int i=0; i<_numParams; i++)
						sums[i] += cur.ParamVector[i];
				for (int i=0; i<_numParams; i++)
					sums[i] /= elites.Count;
				_mean = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(sums);

				// Progress to the next generation
				_population.Clear();
			}
		}
	}
}
