using TMPro;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class UnitBoardCardView : BoardCardView
    {
        protected TextMeshPro AttackText;

        protected SpriteRenderer TypeSprite;

        protected TextMeshPro DefenseText;

        public UnitBoardCardView(GameObject selfObject, CardModel cardModel)
            : base(selfObject, cardModel)
        {
            AttackText = selfObject.transform.Find("AttackText").GetComponent<TextMeshPro>();
            DefenseText = selfObject.transform.Find("DefenseText").GetComponent<TextMeshPro>();

            Transform cardBack = selfObject.transform.Find("Back");

            if (cardBack != null)
            {
                IGameplayManager gameplayManager = GameClient.Get<IGameplayManager>();

                if (gameplayManager.CurrentPlayerDeck != null)
                {
                    if (gameplayManager.CurrentPlayerDeck.Back == 1)
                    {
                        cardBack.GetComponent<SpriteRenderer>().sprite = 
                        GameClient.Get<ILoadObjectsManager>().GetObjectByPath<Sprite>("Images/UI/CardBack/CZB_Card_Back_Backer");
                    }
                    else if (gameplayManager.CurrentPlayerDeck.Back == 0)
                    {
                        cardBack.GetComponent<SpriteRenderer>().sprite = 
                        GameClient.Get<ILoadObjectsManager>().GetObjectByPath<Sprite>("Images/UI/CardBack/cardback");
                    }
                }
            }

            DrawStats();

            // TODO: refactor-state: unsubscribe
            Model.UnitDamageChanged += InstanceCardOnStatChanged;
            Model.UnitDefenseChanged += InstanceCardOnStatChanged;
        }

        private void InstanceCardOnStatChanged(int oldValue, int newValue)
        {
            DrawStats();
        }

        private void DrawStats()
        {
            AttackText.text = Model.CurrentDamage.ToString();
            DefenseText.text = Model.CurrentDefense.ToString();

            FillColor(Model.CurrentDamage, Model.Card.Prototype.Damage, AttackText);
            FillColor(Model.CurrentDefense, Model.Card.Prototype.Defense, DefenseText);
        }

        public void DrawOriginalStats()
        {
            AttackText.text = Model.Card.Prototype.Damage.ToString();
            DefenseText.text = Model.Card.Prototype.Defense.ToString();

            FillColor(Model.Card.Prototype.Damage, Model.Card.Prototype.Damage, AttackText);
            FillColor(Model.Card.Prototype.Defense, Model.Card.Prototype.Defense, DefenseText);
        }

        private void FillColor(int stat, int initialStat, TextMeshPro text)
        {
            if (stat > initialStat)
            {
                text.color = Color.green;
            }
            else if (stat < initialStat)
            {
                text.color = Color.red;
            }
            else
            {
                text.color = Color.white;
            }
        }
    }
}
