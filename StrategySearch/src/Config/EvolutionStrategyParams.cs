namespace StrategySearch.Config
{
   class EvolutionStrategyParams
   {
      public int PopulationSize { get; set; }
      public int NumToEvaluate { get; set; }
      public int NumElites { get; set; }
      public double MutationPower { get; set; }
   }
}
