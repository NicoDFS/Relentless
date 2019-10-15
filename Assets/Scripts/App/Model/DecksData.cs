using System.Collections.Generic;
using System.Linq;
using Loom.ZombieBattleground.Common;
using Newtonsoft.Json;
using UnityEngine;

namespace Loom.ZombieBattleground.Data
{
    public class DecksData
    {
        public List<Deck> Decks { get; }

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
        public DeckId Id { get; set; }

        [JsonProperty("HeroId")]
        public OverlordId OverlordId { get; set; }
        public string Name { get { return ActualName; } set 
            {
                string[] nameSplit = value.Split('|');
                if (nameSplit.Length > 1)
                {
                    FinalName = "";
                    for (int i = 0; i < nameSplit.Length-1; i++)
                    {
                        FinalName += nameSplit[i];
                    }
                    int finalBack = 0;
                    {
                        int.TryParse(nameSplit[nameSplit.Length-1], out finalBack);
                    }
                    Back = finalBack;
                }
                else
                {
                    FinalName = nameSplit[0];
                    Back = 0;
                }

                ActualName = value;
            } 
        }
        public int Back { get; set; }

        public string FinalName {get; set;}

        private string ActualName {get; set;}

        public List<DeckCardData> Cards { get; set; }

        public Enumerators.Skill PrimarySkill { get; set; }

        public Enumerators.Skill SecondarySkill { get; set; }

        public Deck(
            DeckId id,
            OverlordId overlordId,
            string name,
            List<DeckCardData> cards,

            Enumerators.Skill primarySkill,
            Enumerators.Skill secondarySkill
            )
        {
            Id = id;
            OverlordId = overlordId;
            Name = name;


            Cards = cards ?? new List<DeckCardData>();
            PrimarySkill = primarySkill;
            SecondarySkill = secondarySkill;
        }

        public void AddCard(CardKey cardKey)
        {
            bool wasAdded = false;
            foreach (DeckCardData card in Cards)
            {
                if (card.CardKey == cardKey)
                {
                    card.Amount++;
                    wasAdded = true;
                }
            }

            if (!wasAdded)
            {
                DeckCardData cardData = new DeckCardData(cardKey, 1);
                Cards.Add(cardData);
            }
        }

        public void RemoveCard(CardKey cardKey)
        {
            foreach (DeckCardData card in Cards)
            {
                if (card.CardKey == cardKey)
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
                OverlordId,
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
        public CardKey CardKey { get; set; }

        public int Amount { get; set; }

        public DeckCardData(CardKey cardKey, int amount)
        {
            CardKey = cardKey;
            Amount = amount;
        }

        public DeckCardData Clone()
        {
            return new DeckCardData(CardKey, Amount);
        }
    }
}
