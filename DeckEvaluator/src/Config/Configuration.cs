namespace DeckEvaluator.Config
{
   class Configuration
   {
      public EvaluationParams Evaluation { get; set; }
      public NetworkParams Network { get; set; }
      public NerfParams[] Nerfs { get; set; }
   }

   class EvaluationParams
   {
      public string OpponentDeckSuite { get; set; }
      public string[] DeckPools { get; set; }
      public PlayerStrategyParams[] PlayerStrategies { get; set; }
   }

   class PlayerStrategyParams
   {
      public int NumGames { get; set; }
      public string Strategy { get; set; }
   }

   class NetworkParams
   {
      public int[] LayerSizes { get; set; }
   }

   class NerfParams
   {
      public string CardName { get; set; }
      public int NewManaCost { get; set; }
      public int NewAttack { get; set; }
      public int NewHealth { get; set; }
   }
}
