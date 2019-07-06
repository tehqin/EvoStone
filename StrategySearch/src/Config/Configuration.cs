namespace StrategySearch.Config
{
   class Configuration
   {
   	public EvaluationParams Evaluation { get; set; }
      public NetworkParams Network { get; set; }
      public PlayerParams Player { get; set; }
      public SearchParams Search { get; set; }
   }

   class EvaluationParams
   {
      public string[] DeckPools { get; set; }
      public PlayerStrategyParams[] PlayerStrategies { get; set; }
   }

   class PlayerStrategyParams
   {
      public string Strategy { get; set; }
   }

   class SearchParams
   {
      public string Type { get; set; }
      public string ConfigFilename { get; set; }
   }

   class NetworkParams
   {
      public int[] LayerSizes { get; set; }
   }

	class PlayerParams
	{
      public string DeckPool { get; set; }
      public string DeckName { get; set; }
	}
}
