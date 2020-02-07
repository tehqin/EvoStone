namespace DeckSearch.Config
{
   class Configuration
   {
      public DeckspaceParams Deckspace { get; set; }
      public SearchParams Search { get; set; }
      public MapParams Map { get; set; }
   }

   class DeckspaceParams
   {
      public string HeroClass { get; set; }
      public string[] CardSets { get; set; }
   }

   class SearchParams
   {
      public int InitialPopulation { get; set; }
      public int NumToEvaluate { get; set; }
   }

   class MapParams
   {
      public string Type { get; set; }
      public int RemapFrequency { get; set; }
      public int StartSize { get; set; }
      public int EndSize { get; set; }

      public FeatureParams[] Features { get; set; }
   }

   class FeatureParams
   {
      public string Name { get; set; }
      public int MinValue { get; set; } 
      public int MaxValue { get; set; } 
   }
}
