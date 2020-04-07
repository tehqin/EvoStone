namespace DeckSearch.Config
{
   class Configuration
   {
      public DeckspaceParams Deckspace { get; set; }
      public SearchParams Search { get; set; }
   }

   class DeckspaceParams
   {
      public string HeroClass { get; set; }
      public string[] CardSets { get; set; }
   }

   class SearchParams
   {
      public string Type { get; set; }
      public string ConfigFilename { get; set; }
   }
}
