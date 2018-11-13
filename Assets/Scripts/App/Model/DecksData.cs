using System.Collections.Generic;
using System.Linq;
using Loom.ZombieBattleground.Common;

namespace Loom.ZombieBattleground.Data
{
    public class DecksData
    {
        public List<Deck> Decks { get; private set; }

        public DecksData(List<Deck> decks)
        {
            Decks = decks;
        }
    }

    public class AIDeck
    {
        public Deck Deck { get; }

        public Enumerators.AIType Type { get; }

        public AIDeck(Deck deck, Enumerators.AIType type)
        {
            Deck = deck;
            Type = type;
        }
    }

    public class Deck
    {
        public long Id { get; set; }

        public int HeroId { get; set; }

        public string Name { get; set; }

        public List<DeckCardData> Cards;

        public int PrimarySkill { get; set; }

        public int SecondarySkill { get; set; }

        public Deck(
            long id,
            int heroId,
            string name,
            List<DeckCardData> cards,
            int primarySkill,
            int secondarySkill
            )
        {
            Id = id;
            HeroId = heroId;
            Name = name;
            Cards = cards ?? new List<DeckCardData>();
            PrimarySkill = primarySkill;
            SecondarySkill = secondarySkill;
        }

        public void AddCard(string cardId)
        {
            bool wasAdded = false;
            foreach (DeckCardData card in Cards)
            {
                if (card.CardName == cardId)
                {
                    card.Amount++;
                    wasAdded = true;
                }
            }

            if (!wasAdded)
            {
                DeckCardData cardData = new DeckCardData(cardId, 1);
                Cards.Add(cardData);
            }
        }

        public void RemoveCard(string cardId)
        {
            foreach (DeckCardData card in Cards)
            {
                if (card.CardName == cardId)
                {
                    card.Amount--;
                    if (card.Amount < 1)
                    {
                        Cards.Remove(card);
                        break;
                    }
                }
            }
        }

        public int GetNumCards()
        {
            int amount = 0;
            foreach (DeckCardData card in Cards)
            {
                amount += card.Amount;
            }

            return amount;
        }

        public Deck Clone()
        {
            Deck deck = new Deck
            (
                Id,
                HeroId,
                Name,
                Cards.Select(c => c.Clone()).ToList(),
                PrimarySkill,
                SecondarySkill
            );
            return deck;
        }
    }

    public class DeckCardData
    {
        public string CardName { get; set; }

        public int Amount { get; set; }

        public DeckCardData(string cardName, int amount)
        {
            CardName = cardName;
            Amount = amount;
        }

        public DeckCardData Clone()
        {
            DeckCardData deckCardData = new DeckCardData
            (
                CardName,
                Amount
            );
            return deckCardData;
        }
    }
}
