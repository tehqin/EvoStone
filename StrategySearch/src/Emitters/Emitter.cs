using StrategySearch.Search;

namespace StrategySearch.Emitters
{
   interface Emitter
   {
      int NumReleased { get; set; }
      Individual GenerateIndividual();
      void ReturnEvaluatedIndividual(Individual ind);
   }
}
