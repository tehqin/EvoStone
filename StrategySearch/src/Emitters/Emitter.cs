using StrategySearch.Search;

namespace StrategySearch.Emitters
{
   interface Emitter
   {
      bool IsBlocking();
      int NumReleased { get; set; }
      Individual GenerateIndividual();
      void ReturnEvaluatedIndividual(Individual ind);
   }
}
