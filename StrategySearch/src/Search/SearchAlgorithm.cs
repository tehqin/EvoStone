namespace StrategySearch.Search
{
   interface SearchAlgorithm
   {
      bool IsRunning();
      Individual GenerateIndividual();
      void ReturnEvaluatedIndividual(Individual ind);
   }
}
