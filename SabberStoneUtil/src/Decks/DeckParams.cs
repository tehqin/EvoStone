using System.Collections.Generic;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

namespace SabberStoneUtil.Decks
{
   public class DeckParams
   {
      public string DeckName { get; set; }
      public string ClassName { get; set; }
      public string[] CardList { get; set; }

      public Deck ContructDeck()
      {
         return new Deck(ClassName, CardList);
      }
   }
}
