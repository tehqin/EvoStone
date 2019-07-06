using SabberStoneUtil.Decks;

namespace SabberStoneUtil.Messaging
{
   public class PlayMatchesMessage
   {
      public DeckParams Deck { get; set; }
      public CustomStratWeights Strategy { get; set; }
   }
}
