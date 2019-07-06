using System;

using SabberStoneCoreAi.Score;

using SabberStoneUtil.Decks;
using SabberStoneUtil.Messaging;

using DeckEvaluator.Config;

namespace DeckEvaluator.Evaluation
{
   class PlayerSetup
   {
      public Deck Deck { get; private set; }
      public Score Strategy { get; private set; }

      public PlayerSetup(Deck deck,
                         Score strategy)
      {
         Deck = deck;
         Strategy = strategy;
      }

      public static Score GetStrategy(string name, 
                                      NetworkParams netParams,
                                      CustomStratWeights weights)
      {
         if (name.Equals("Aggro"))
            return new AggroScore();
         if (name.Equals("Control"))
            return new ControlScore();
         if (name.Equals("Fatigue"))
            return new FatigueScore();
         if (name.Equals("MidRange"))
            return new MidRangeScore();
         if (name.Equals("Ramp"))
            return new RampScore();
         if (name.Equals("Custom"))
            return new CustomScore(weights);
         if (name.Equals("NeuralNet"))
            return new NeuralNetScore(netParams.LayerSizes, weights);

         Console.WriteLine("Strategy "+name+" not a valid strategy.");
         return null;
      }
   }
}
