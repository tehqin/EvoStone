using System;

namespace DeckEvaluator.NeuralNet
{
   public class FullyConnectedNetwork : Network
   {
      private int[] _layerSizes;
      private double[][,] _weights;

      public int NumWeights {get; private set;}
      
      public FullyConnectedNetwork(int[] layerSizes)
      {
         _layerSizes = new int[layerSizes.Length]; 
         Array.Copy(layerSizes, _layerSizes, layerSizes.Length);

         NumWeights = 0;
         _weights = new double[_layerSizes.Length-1][,];
         for (int i=0; i<_layerSizes.Length-1; i++)
         {
            _weights[i] = new double[_layerSizes[i], _layerSizes[i+1]];
            NumWeights += _layerSizes[i] * _layerSizes[i+1];
         }
      }

      public void SetWeights(double[] weightVector)
      {
         if (weightVector.Length != NumWeights)
         {
            Console.WriteLine(String.Format("Num Weight Mismatch {0} vs {1}", weightVector.Length, NumWeights));
         }

         int counter = 0;
         for (int k=0; k<_layerSizes.Length-1; k++)
         {
            for (int i=0; i<_layerSizes[k]; i++)
            {
               for (int j=0; j<_layerSizes[k+1]; j++)
               {
                  _weights[k][i,j] = weightVector[counter];
                  counter++;
               }
            }
         }
      }

      public double[] Evaluate(double[] input)
      {
         if (input.Length != _layerSizes[0])
         {
            Console.WriteLine("Input layer size doesn't match input vector");
            return null;
         }

         var layer = input;
         for (int k=0; k<_layerSizes.Length-1; k++)
         {
            var nextLayer = new double[_layerSizes[k+1]];
            for (int i=0; i<_layerSizes[k]; i++)
               for (int j=0; j<_layerSizes[k+1]; j++)
                  nextLayer[j] += layer[i] * _weights[k][i,j];
         
            for (int j=0; j<_layerSizes[k+1]; j++)
               nextLayer[j] = Math.Tanh(nextLayer[j]);
            layer = nextLayer;
         }

         return layer;
      }
   }
}
