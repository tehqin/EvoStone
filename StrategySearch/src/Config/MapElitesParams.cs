namespace StrategySearch.Config
{
   class MapElitesParams
   {
      public MapElitesSearchParams Search { get; set; }
      public MapParams Map { get; set; }
   }

   class MapElitesSearchParams
   {
      public int InitialPopulation { get; set; }
      public int NumToEvaluate { get; set; }
      public double MutationPower { get; set; }
   }
}
