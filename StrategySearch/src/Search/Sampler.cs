using System;

namespace StrategySearch.Search
{
   class Sampler
   {
      private static Random rnd = new Random();
      public static double gaussian()
      {
         double u1 = 1.0 - rnd.NextDouble();
         double u2 = 1.0 - rnd.NextDouble();
         return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
      }
   }
}
