using System;
using System.Collections.Generic;

using SabberStoneCoreAi.Score;

using SabberStoneUtil.Decks;

using DeckEvaluator.Config;

namespace DeckEvaluator.Evaluation
{
   class GameSuite
   {
      private OpponentParams[] _opponentTypes;
      private DeckPoolManager _deckPools;

      public GameSuite(OpponentParams[] opponentTypes, 
                       DeckPoolManager deckPools)
      {
         _opponentTypes = opponentTypes;
         _deckPools = deckPools;
      }

      // Get a suite of opponents for the desired number of games
      public List<PlayerSetup> GetOpponents(int numGames)
      {
         int curOpponentType = 0;
         int numTakenOfType = 0;
         var opponents = new List<PlayerSetup>();

         // Distribute the games in a round robin fashion
         // based on the portions listed in the config file.
         while (opponents.Count < numGames)
         {
            if (numTakenOfType >= _opponentTypes[curOpponentType].Portion)
            {
               curOpponentType = 
                  (curOpponentType+1) % _opponentTypes.Length; 
               numTakenOfType = 0;
            }
            
            OpponentParams curType = _opponentTypes[curOpponentType];
            Deck deck = _deckPools.GetDeck(curType.DeckPool, 
                  curType.DeckName);
            var opponent = new PlayerSetup(deck, 
                  PlayerSetup.GetStrategy(curType.Strategy, null, null));
            opponents.Add(opponent);
            numTakenOfType++;
         }

         return opponents;
      }
   }
}
