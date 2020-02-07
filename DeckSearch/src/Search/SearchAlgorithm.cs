using System.Collections.Generic;
using SabberStoneCore.Model;

namespace DeckSearch.Search
{
   interface SearchAlgorithm
   {
      bool IsRunning();
      bool IsBlocking();
      Individual GenerateIndividual(List<Card> cardSet);
      void ReturnEvaluatedIndividual(Individual ind);
   }
}
