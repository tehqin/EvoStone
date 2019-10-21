namespace StrategySearch.Config
{
   class EmitterParams
   {
      public string Type { get; set; }
      public int Count { get; set; }
      public double OverflowFactor { get; set; }
      public int PopulationSize { get; set; }
      public int NumParents { get; set; }
      public double MutationPower { get; set; }
   }
}
