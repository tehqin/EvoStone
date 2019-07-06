namespace StrategySearch.Config
{
   class CMA_ES_Params
   {
      public int PopulationSize { get; set; }
      public int NumToEvaluate { get; set; }
      public int NumElites { get; set; }
      public double MutationScalar { get; set; }
   }
}
