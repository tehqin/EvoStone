using System;

using SabberStoneUtil.Messaging;

namespace StrategySearch.Search
{
   class Individual
   {
      public int ID { get; set; }
      public int EmitterID { get; set; }
      public int Generation { get; set; }
      
      public OverallStatistics OverallData { get; set; }
      public StrategyStatistics[] StrategyData { get; set; }
      
      public double Fitness { get; set; }
      public double Delta { get; set; }
      public bool IsNovel { get; set; }
      public double[] ParamVector { get; set; }
      public double[] Features { get; set; }

      public Individual(int numParams)
      {
         ParamVector = new double[numParams];
      }

      public double GetStatByName(string name)
      {
         return OverallData.GetStatByName(name);
      }

      // Unpack the vector searched by the evolution strategy
      public CustomStratWeights GetWeights()
      {
         return CustomStratWeights.CreateFromVector(ParamVector);
      }
   }
}
