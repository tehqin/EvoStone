using System;
using System.Collections.Generic;
using System.Linq;
using SabberStoneCore.Model.Zones;
using SabberStoneCore.Model.Entities;

using SabberStoneUtil.Messaging;

using DeckEvaluator.NeuralNet;

namespace SabberStoneCoreAi.Score
{
	public class NeuralNetScore : SabberStoneCoreAi.Score.Score
	{
      private Network _network;

      public NeuralNetScore(int[] layerSizes, CustomStratWeights weights)
      {
         _network = new FullyConnectedNetwork(layerSizes);
         _network.SetWeights(weights.Weights);
      }

		public override int Rate()
		{
         // Hard guard the win conditions
			if (OpHeroHp < 1)
				return Int32.MaxValue;
			if (HeroHp < 1)
				return Int32.MinValue;

         var inputVector = new double[15];
         inputVector[0] = HeroHp;
         inputVector[1] = OpHeroHp;
         inputVector[2] = HeroAtk;
         inputVector[3] = OpHeroAtk;
         inputVector[4] = HandTotCost;
         inputVector[5] = HandCnt;
         inputVector[6] = OpHandCnt;
         inputVector[7] = DeckCnt;
         inputVector[8] = OpDeckCnt;

         // Minion stats
         inputVector[9] = MinionTotAtk;
         inputVector[10] = OpMinionTotAtk;
         inputVector[11] = MinionTotHealth;
         inputVector[12] = OpMinionTotHealth;
         inputVector[13] = MinionTotHealthTaunt;
         inputVector[14] = OpMinionTotHealthTaunt;

         double result = _network.Evaluate(inputVector)[0];
         result *= 1000000;
         return (int)result;
		}

		public override Func<List<IPlayable>, List<int>> MulliganRule()
		{
			return p => p.Where(t => t.Cost > 3).Select(t => t.Id).ToList();
		}
	}
}
