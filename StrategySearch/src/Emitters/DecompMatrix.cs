using System;
using System.Linq;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;
using LA = MathNet.Numerics.LinearAlgebra;

namespace StrategySearch.Emitters
{
   class DecompMatrix
   {
      private int _numDimensions;

      public double ConditionNumber;
      public Matrix<double> C { get; set; }
      public Matrix<double> Invsqrt { get; set; }
      public Matrix<double> Eigenbasis { get; set; }
      public LA.Vector<double> Eigenvalues { get; set; }

      public DecompMatrix(int numDimensions)
      {
         _numDimensions = numDimensions;

         ConditionNumber = 1.0;
         C = DenseMatrix.CreateIdentity(_numDimensions);
         Eigenbasis = DenseMatrix.CreateIdentity(_numDimensions);
         Eigenvalues = LA.Vector<double>.Build.Dense(_numDimensions, i => 1.0);
         Invsqrt = DenseMatrix.CreateIdentity(_numDimensions);
      }

      public void UpdateEigensystem()
      {
         Evd<double> evd = C.Evd();
         Eigenvalues = 
            DenseVector.OfEnumerable(evd.EigenValues.Select(c => c.Real));
         Eigenbasis = evd.EigenVectors;

         for (int i=0; i<_numDimensions; i++)
         {
            for (int j=0; j<=i; j++)
            {
               double sum = 0;
               for (int k=0; k<_numDimensions; k++)
               {
                  sum += Eigenbasis[i,k] * Eigenbasis[j,k] / Math.Sqrt(Eigenvalues[k]);
               }

               Invsqrt[i,j] = Invsqrt[j,i] = sum;
            }
         }
      }
   }
}
