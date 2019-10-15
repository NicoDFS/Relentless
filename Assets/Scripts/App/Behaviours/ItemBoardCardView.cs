using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class ItemBoardCardView : BoardCardView
    {
        public ItemBoardCardView(GameObject selfObject, CardModel cardModel)
            : base(selfObject, cardModel)
        {
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
        }
    }
}
