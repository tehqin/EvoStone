using System.Collections.Generic;

using DeckSearch.Search;

namespace DeckSearch.Mapping
{
   interface FeatureMap
   {
      int NumGroups { get; }
      int NumFeatures { get; }
      Dictionary<string, Individual> EliteMap { get; }
      Dictionary<string, int> CellCount { get; }

      void Add(Individual toAdd);
      Individual GetRandomElite();
   }
}
