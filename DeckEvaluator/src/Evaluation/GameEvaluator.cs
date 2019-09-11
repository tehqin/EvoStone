using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks;
using SabberStoneCore.Tasks.PlayerTasks;

using SabberStoneCoreAi.Nodes;
using SabberStoneCoreAi.Score;

namespace DeckEvaluator.Evaluation
{
   class GameEvaluator
   {
      public struct GameResult
      {
         public bool _didWin;
         public Dictionary<string, int> _cardUsage;
         public int _healthDifference;
         public int _damageDone;
         public int _numTurns;
         public int _cardsDrawn;
         public int _handSize;
         public int _manaSpent;
         public int _manaWasted;
         public int _strategyAlignment;

         public GameResult(bool didWin, Dictionary<string, int> cardUsage,
                           int healthDifference, int damageDone, 
                           int numTurns, int cardsDrawn, int handSize,
                           int manaSpent, int manaWasted,
                           int strategyAlignment)
         {
            _didWin = didWin;
            _cardUsage = cardUsage;
            _healthDifference = healthDifference;
            _damageDone = damageDone;
            _numTurns = numTurns;
            _cardsDrawn = cardsDrawn;
            _handSize = handSize;
            _manaSpent = manaSpent;
            _manaWasted = manaWasted;
            _strategyAlignment = strategyAlignment;
         }
      }

      private PlayerSetup _player;
      private PlayerSetup _opponent;
      private Dictionary<string, int> _cardUsage; 

		public GameEvaluator(PlayerSetup player, PlayerSetup opponent)
		{
         _player = player;
         _opponent = opponent;
         _cardUsage = new Dictionary<string, int>();
      }

      public void updateUsage(Card playedCard)
      {
         string cardName = playedCard.Name.ToString();
         if (_cardUsage.ContainsKey(cardName))
         {
            _cardUsage[cardName] += 1;
         }
         else
         {
            _cardUsage.Add(cardName, 1);
         }
      }

      private bool _randomHappened;

      public void RandomHappended(object sender, bool happened)
      {
         _randomHappened = happened;
      }

      public GameResult PlayGame()
      {
         _randomHappened = false;
         
         var game = new Game(
            new GameConfig()
               {
                  StartPlayer = 1,
                  Player1Name = "Player",
                  Player1HeroClass = _player.Deck.DeckClass,
                  Player1Deck = _player.Deck.CardList,
                  Player2Name = "Opponent",
                  Player2HeroClass = _opponent.Deck.DeckClass,
                  Player2Deck = _opponent.Deck.CardList,
                  FillDecks = false,
                  Shuffle = true,
                  SkipMulligan = false
               });

         // Register for random actions in the Hearthstone game.
         // This prevents the game from crashing by replanning when 
         // random occurs.
         game.RandomHappenedEvent += RandomHappended;

         int maxDepth = 13;
         int maxWidth = 4;

         game.StartGame();

         var aiPlayer1 = _player.Strategy;
         var aiPlayer2 = _opponent.Strategy;

         List<int> mulligan1 = aiPlayer1.MulliganRule().Invoke(game.Player1.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList());
         List<int> mulligan2 = aiPlayer2.MulliganRule().Invoke(game.Player2.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList());

         //Console.WriteLine($"Player1: Mulligan {string.Join(",", mulligan1)}");
         //Console.WriteLine($"Player2: Mulligan {string.Join(",", mulligan2)}");

         game.Process(ChooseTask.Mulligan(game.Player1, mulligan1));
         game.Process(ChooseTask.Mulligan(game.Player2, mulligan2));

         game.MainReady();
         int totalDamage = 0;
         int totalCardsDrawn = 0;
         int totalManaSpent = 0;
         int totalManaWasted = 0;
         int totalOptionScore = 0;
         int totalHandSize = 0;

         while (game.State != State.COMPLETE)
         {
            /*
            Console.WriteLine($"Player1: {game.Player1.PlayState} / Player2: {game.Player2.PlayState} - " +
                              "ROUND {(game.Turn + 1) / 2} - {game.CurrentPlayer.Name}");
            Console.WriteLine($"Hero[P1]: {game.Player1.Hero.Health} / Hero[P2]: {game.Player2.Hero.Health}");
            Console.WriteLine("");
            */

            int numCardsDrawn = 1;
            while (game.State == State.RUNNING && game.CurrentPlayer == game.Player1)
            {
               //Console.WriteLine("* Calculating solutions *** Player 1 ***");
               List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player1.Id, aiPlayer1, maxDepth, maxWidth);

               var solution = new List<PlayerTask>();
               OptionNode bestOption = solutions.OrderByDescending(p => p.Score).First();
               bestOption.PlayerTasks(ref solution);

               if (bestOption.Score != Int32.MaxValue &&
                   bestOption.Score != Int32.MinValue)
               {
                  totalOptionScore += bestOption.Score;
               }

               //Console.WriteLine($"- Player 1 - <{game.CurrentPlayer.Name}> ---------------------------");
               foreach (PlayerTask task in solution)
               {
                  // Need to count this before processing the task
                  // (in the case of an endturn task.)
                  int cardsDrawnThisTurn = task.Controller.NumCardsDrawnThisTurn;
                  
                  //Console.WriteLine(task.FullPrint());
                  if (!game.Process(task))
                     break;

                  // Record some stats
                  numCardsDrawn = Math.Max(numCardsDrawn, cardsDrawnThisTurn);
                  if (task.PlayerTaskType == PlayerTaskType.PLAY_CARD)
                  {
                     updateUsage(task.Source.Card);
                  }
                  if (task.PlayerTaskType == PlayerTaskType.MINION_ATTACK)
                  {
                     int damageTaken = ((ICharacter)task.Source).AttackDamage;
                     totalDamage += damageTaken;
                  }

                  if (game.CurrentPlayer.Choice != null || _randomHappened)
                  {
                     _randomHappened = false;
                     //Console.WriteLine("* Recalculating due to randomness or final solution ...");
                     break;
                  }
               }
               
               totalManaSpent += game.Player1.UsedMana;
               totalManaWasted += (game.Player1.BaseMana - game.Player1.UsedMana);
            }

            totalCardsDrawn += numCardsDrawn;
            totalHandSize += game.Player1.HandZone.Count;

            // Random mode for Player 2
            //Console.WriteLine($"- Player 2 - <{game.CurrentPlayer.Name}> ---------------------------");
            while (game.State == State.RUNNING && game.CurrentPlayer == game.Player2)
            {
               //Console.WriteLine("* Calculating solutions *** Player 2 ***");
               List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player2.Id, aiPlayer2, maxDepth, maxWidth);
               var solution = new List<PlayerTask>();
               solutions.OrderByDescending(p => p.Score).First().PlayerTasks(ref solution);
               //Console.WriteLine($"- Player 2 - <{game.CurrentPlayer.Name}> ---------------------------");
               foreach (PlayerTask task in solution)
               {
                  //Console.WriteLine(task.FullPrint());
                  game.Process(task);
                  if (game.CurrentPlayer.Choice != null || _randomHappened)
                  {
                     _randomHappened = false;
                     //Console.WriteLine("* Recalculating due to randomness or final solution ...");
                     break;
                  }
               }
            }
         }

         //Console.WriteLine($"\nGame: {game.State}, Player1: {game.Player1.PlayState} / Player2: {game.Player2.PlayState}\n");

         // Calculate the number of turns we took.
         int numTurns = (1+game.Turn) / 2;

         bool didWin = game.Player1.PlayState == PlayState.WON;
         return new GameResult(didWin, _cardUsage,
                       game.Player1.Hero.Health-game.Player2.Hero.Health,
                       totalDamage, numTurns, totalCardsDrawn, 
                       totalHandSize, totalManaSpent, totalManaWasted,
                       totalOptionScore);
      }
   }
}
