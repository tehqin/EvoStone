using System;
using System.Collections.Generic;

using Nett;

namespace SabberStoneUtil.Decks
{
   public class DeckPoolManager
   {
      private Dictionary<string, Dictionary<string, Deck>> _deckPools;

		public DeckPoolManager()
      {
         _deckPools = new Dictionary<string, Dictionary<string, Deck>>();
      }

      public void AddDeckPool(DeckPoolParams config)
      {
         // For each entry in this deck pool, contruct a mapping from
         // the name of the deck to the class and card listing.
         var deckMap = new Dictionary<string, Deck>();
         foreach (DeckParams curDeckParams in config.Decks)
         {
            deckMap.Add(curDeckParams.DeckName,
                        curDeckParams.ContructDeck());
         }

         // Add this deck pool to the map of pools.
         _deckPools.Add(config.PoolName, deckMap);
      }

      public void AddDeckPools(string[] deckPoolFilenames)
      {
         foreach (string poolFilename in deckPoolFilenames)
         {
            var config = Toml.ReadFile<DeckPoolParams>(poolFilename);
            AddDeckPool(config);
         }
      }

      public Deck GetDeck(string poolName, string deckName)
      {
         return _deckPools[poolName][deckName];
      }
   }
}
