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
      public int WinCount { get; set; }
      public int TotalHealthDifference { get; set; }
      public int DamageDone { get; set; }
      public int NumTurns { get; set; }
      public int CardsDrawn { get; set; }
      public int HandSize { get; set; }
      public int ManaSpent { get; set; }
      public int ManaWasted { get; set; }
      public int StrategyAlignment { get; set; }
      public int Dust { get; set; }
      public int DeckManaSum { get; set; }
      public int DeckManaVariance { get; set; }
      public int NumMinionCards { get; set; }
      public int NumSpellCards { get; set; }
   
      public void Accumulate(OverallStatistics rhs)
      {
         for (int i=0; i<UsageCounts.Length; i++)
            UsageCounts[i] += rhs.UsageCounts[i];
         WinCount += rhs.WinCount;
         TotalHealthDifference += rhs.TotalHealthDifference;
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

		public int GetStatByName(string name)
      {
         if (name.Equals("WinCount"))
            return WinCount;
         if (name.Equals("TotalHealthDifference"))
            return TotalHealthDifference;
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
            "TotalHealthDifference",
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
      public int WinCount { get; set; }
      public int Alignment { get; set; }

      public int GetStatByName(string name)
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
