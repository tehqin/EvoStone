namespace StrategySearch.Config
{
   class CMA_ME_Params
   {
      public CMA_ME_SearchParams Search { get; set; }
      public MapParams Map { get; set; }
      public EmitterParams[] Emitters { get; set; }
   }

   class CMA_ME_SearchParams
   {
      public int NumToEvaluate { get; set; }
   }
}
