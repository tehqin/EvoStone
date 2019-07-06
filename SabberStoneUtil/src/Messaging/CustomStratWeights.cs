using System;

namespace SabberStoneUtil.Messaging
{
   public class CustomStratWeights
   {
      public double[] Weights { get; set; }
		
	   public static CustomStratWeights CreateFromVector(double[] vec)
		{  
			var weights = new CustomStratWeights();

         weights.Weights = new double[vec.Length];
         Array.Copy(vec, weights.Weights, vec.Length);

			return weights;
		}

		public double GetWeightByName(string name)
      {
         if (name.Equals("HeroHp"))
            return Weights[0];
         if (name.Equals("OpHeroHp"))
            return Weights[1];
         if (name.Equals("HeroAtk"))
            return Weights[2];
         if (name.Equals("OpHeroAtk"))
            return Weights[3];
         if (name.Equals("HandTotCost"))
            return Weights[4];
         if (name.Equals("HandCnt"))
            return Weights[5];
         if (name.Equals("OpHandCnt"))
            return Weights[6];
         if (name.Equals("DeckCnt"))
            return Weights[7];
         if (name.Equals("OpDeckCnt "))
            return Weights[8];

         if (name.Equals("MinionTotAtk"))
            return Weights[9];
         if (name.Equals("OpMinionTotAtk"))
            return Weights[10];
         if (name.Equals("MinionTotHealth"))
            return Weights[11];
         if (name.Equals("OpMinionTotHealth"))
            return Weights[12];
         if (name.Equals("MinionTotHealthTaunt"))
            return Weights[13];
         if (name.Equals("OpMinionTotHealthTaunt"))
            return Weights[14];

         return Double.MinValue;
      }

      public static string[] Properties = new[] {
            "HeroHp",
            "OpHeroHp",
            "HeroAtk",
            "OpHeroAtk",
            "HandTotCost",
            "HandCnt",
            "OpHandCnt",
            "DeckCnt",
            "OpDeckCnt ",

            "MinionTotAtk",
            "OpMinionTotAtk",
            "MinionTotHealth",
            "OpMinionTotHealth",
            "MinionTotHealthTaunt",
            "OpMinionTotHealthTaunt",
         };
	}
}
