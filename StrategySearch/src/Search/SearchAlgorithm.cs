namespace StrategySearch.Search
{
   interface SearchAlgorithm
   {
      bool IsRunning();
      bool IsBlocking();
      Individual GenerateIndividual();
      void ReturnEvaluatedIndividual(Individual ind);
   }
}
