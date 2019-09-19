using System;

using SabberStoneUtil.Decks;

namespace SabberStoneUtil.Messaging
{
   public class ResultsMessage
   {
      public DeckParams PlayerDeck { get; set; }
		public OverallStatistics OverallStats { get; set; }
		public StrategyStatistics[] StrategyStats { get; set; }
   }

   public class OverallStatistics
   {
      public int[] UsageCounts { get; set; }
      public double WinCount { get; set; }
      public double AverageHealthDifference { get; set; }
      public double DamageDone { get; set; }
      public double NumTurns { get; set; }
      public double CardsDrawn { get; set; }
      public double HandSize { get; set; }
      public double ManaSpent { get; set; }
      public double ManaWasted { get; set; }
      public double StrategyAlignment { get; set; }
      public double Dust { get; set; }
      public double DeckManaSum { get; set; }
      public double DeckManaVariance { get; set; }
      public double NumMinionCards { get; set; }
      public double NumSpellCards { get; set; }
   
      public void Accumulate(OverallStatistics rhs)
      {
         for (int i=0; i<UsageCounts.Length; i++)
            UsageCounts[i] += rhs.UsageCounts[i];
         WinCount += rhs.WinCount;
         AverageHealthDifference += rhs.AverageHealthDifference;
         DamageDone += rhs.DamageDone;
         NumTurns += rhs.NumTurns;
         CardsDrawn += rhs.CardsDrawn;
         HandSize += rhs.HandSize;
         ManaSpent += rhs.ManaSpent;
         ManaWasted += rhs.ManaWasted;
         StrategyAlignment += rhs.StrategyAlignment;
      }

      public void ScaleByNumStrategies(int numStrats)
      {
         DamageDone /= numStrats;
         NumTurns /= numStrats;
         CardsDrawn /= numStrats;
         HandSize /= numStrats;
         ManaSpent /= numStrats;
         ManaWasted /= numStrats;
         StrategyAlignment /= numStrats;
      }

		public double GetStatByName(string name)
      {
         if (name.Equals("WinCount"))
            return WinCount;
         if (name.Equals("AverageHealthDifference"))
            return AverageHealthDifference;
         if (name.Equals("DamageDone"))
            return DamageDone;
         if (name.Equals("NumTurns"))
            return NumTurns;
         if (name.Equals("CardsDrawn"))
            return CardsDrawn;
         if (name.Equals("HandSize"))
            return HandSize;
         if (name.Equals("ManaSpent"))
            return ManaSpent;
         if (name.Equals("ManaWasted"))
            return ManaWasted;
         if (name.Equals("StrategyAlignment"))
            return StrategyAlignment;
         if (name.Equals("Dust"))
            return Dust;
         if (name.Equals("DeckManaSum"))
            return DeckManaSum;
         if (name.Equals("DeckManaVariance"))
            return DeckManaVariance;
         if (name.Equals("NumMinionCards"))
            return NumMinionCards;
         if (name.Equals("NumSpellCards"))
            return NumSpellCards;

         return Int32.MinValue;
      }

      public static string[] Properties = new[] {
            "WinCount",
            "AverageHealthDifference",
            "DamageDone",
            "NumTurns",
            "CardsDrawn",
            "HandSize",
            "ManaSpent",
            "ManaWasted",
            "StrategyAlignment",
            "Dust",
            "DeckManaSum",
            "DeckManaVariance",
            "NumMinionCards",
            "NumSpellCards"
         };
   }

   public class StrategyStatistics
   {
      public double WinCount { get; set; }
      public double Alignment { get; set; }

      public double GetStatByName(string name)
      {
         if (name.Equals("WinCount"))
            return WinCount;
         if (name.Equals("Alignment"))
            return Alignment;

         return Int32.MinValue;
      }

      public static string[] Properties = new[] {
            "WinCount",
            "Alignment"
         };
   }
}
