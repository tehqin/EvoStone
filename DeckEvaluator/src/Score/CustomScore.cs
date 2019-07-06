using System;
using System.Collections.Generic;
using System.Linq;
using SabberStoneCore.Model.Zones;
using SabberStoneCore.Model.Entities;

using SabberStoneUtil.Messaging;

namespace SabberStoneCoreAi.Score
{
	public class CustomScore : SabberStoneCoreAi.Score.Score
	{
      public CustomStratWeights Weights { get; set; }

      public CustomScore(CustomStratWeights weights)
      {
         Weights = weights;
      }

		public override int Rate()
		{
         // Hard guard the win conditions
			if (OpHeroHp < 1)
				return Int32.MaxValue;
			if (HeroHp < 1)
				return Int32.MinValue;

         double result = 0;
			
         result += Weights.GetWeightByName("HeroHp") * HeroHp;
         result += Weights.GetWeightByName("OpHeroHp") * OpHeroHp;
         result += Weights.GetWeightByName("HeroAtk") * HeroAtk;
         result += Weights.GetWeightByName("OpHeroAtk") * OpHeroAtk;
         result += Weights.GetWeightByName("HandTotCost") * HandTotCost;
         result += Weights.GetWeightByName("HandCnt") * HandCnt;
         result += Weights.GetWeightByName("OpHandCnt") * OpHandCnt;
         result += Weights.GetWeightByName("DeckCnt") * DeckCnt;
         result += Weights.GetWeightByName("OpDeckCnt") * OpDeckCnt;

         result += Weights.GetWeightByName("MinionTotAtk") * MinionTotAtk;
         result += Weights.GetWeightByName("OpMinionTotAtk") * OpMinionTotAtk;
         result += Weights.GetWeightByName("MinionTotHealth") * MinionTotHealth;
         result += Weights.GetWeightByName("OpMinionTotHealth") * OpMinionTotHealth;
         result += Weights.GetWeightByName("MinionTotHealthTaunt") * MinionTotHealthTaunt;
         result += Weights.GetWeightByName("OpMinionTotHealthTaunt") * OpMinionTotHealthTaunt;
         
         result *= 1000;
         return (int)result;
		}

		public override Func<List<IPlayable>, List<int>> MulliganRule()
		{
			return p => p.Where(t => t.Cost > 3).Select(t => t.Id).ToList();
		}
	}
}
