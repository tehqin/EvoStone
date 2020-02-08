using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

namespace DeckSearch.Config
{
    class CardReader
    {
        public static CardClass GetClassFromName(string className)
        {
            className = className.ToUpper();
            foreach (CardClass curClass in Cards.HeroClasses)
                if (curClass.ToString().Equals(className))
                    return curClass;

            Console.WriteLine("Card class " + className
                              + " not a valid hero class.");
            return CardClass.NEUTRAL;
        }

        public static CardSet[] GetSetsFromNames(string[] cardSets)
        {
            Console.WriteLine(String.Join(" ", cardSets));
            var sets = new CardSet[cardSets.Length];

            for (int i = 0; i < cardSets.Length; i++)
                foreach (CardSet c in Enum.GetValues(typeof(CardSet)))
                    if (c.ToString().Equals(cardSets[i]))
                        sets[i] = c;

            return sets;
        }


        private static void printCards(List<Card> cards) {
            Console.WriteLine("Card set size = " + cards.Count);
            foreach (Card c in cards) {
                Console.WriteLine(c.Name);
            }
        }

        public static List<Card> GetCards(CardClass heroClass, CardSet[] sets)
        {
            var hs = new HashSet<Card>();
            var cards = new List<Card>();
            List<Card> allCards = Cards.All.ToList();
            foreach (Card c in allCards)
            {
                if (c != Cards.FromName("Default")
                    && c.Implemented
                    && c.Collectible
                    && c.Type != CardType.HERO
                    && c.Type != CardType.ENCHANTMENT
                    && c.Type != CardType.INVALID
                    && c.Type != CardType.HERO_POWER
                    && c.Type != CardType.TOKEN
                    && (c.Class == CardClass.NEUTRAL || c.Class == heroClass)
                    && !hs.Contains(c))
                {
                    bool validSet = false;
                    foreach (CardSet curSet in sets)
                        validSet |= (c.Set == curSet);

                    if (validSet)
                    {
                        cards.Add(c);
                        hs.Add(c);
                    }
                }
            }
            return cards;
        }
    }
}
