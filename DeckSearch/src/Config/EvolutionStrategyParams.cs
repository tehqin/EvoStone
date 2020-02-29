namespace DeckSearch.Config
{
   class EvolutionStrategyParams
   {
      public EvolutionStrategySearchParams Search { get; set; }
   }

   class EvolutionStrategySearchParams
   {
      public int InitialPopulation { get; set; }
      public int NumToEvaluate { get; set; }
      public int NumParents { get; set; }
   }
}
