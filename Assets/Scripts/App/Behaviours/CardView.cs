// Copyright (C) 2016-2017 David Pol. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using System;
using GrandDevs.CZB.Common;
using DG.Tweening;
using GrandDevs.CZB.Data;

namespace GrandDevs.CZB
{
    public class CardView : MonoBehaviour
    {
        public WorkingCard WorkingCard { get; private set; }

        [SerializeField]
        protected SpriteRenderer glowSprite;

        [SerializeField]
        protected SpriteRenderer pictureSprite;

        [SerializeField]
        protected SpriteRenderer backgroundSprite;

        [SerializeField]
        protected TextMeshPro costText;

        [SerializeField]
        protected TextMeshPro nameText;

        [SerializeField]
        protected TextMeshPro bodyText;

        [SerializeField]
        protected TextMeshPro amountText;

        protected GameObject previewCard;

        protected Animator cardAnimator;

        public Card libraryCard;

        public bool isNewCard = false;

        public int manaCost { get; protected set; }

        public ParticleSystem removeCardParticle { get; protected set; }

        public int CurrentTurn { get; set; }

        [HideInInspector]
        public bool isPreview;

        protected virtual void Awake()
        {
            Assert.IsNotNull(glowSprite);
            Assert.IsNotNull(pictureSprite);
            Assert.IsNotNull(costText);
            Assert.IsNotNull(nameText);
            Assert.IsNotNull(bodyText);

            cardAnimator = gameObject.GetComponent<Animator>();
            cardAnimator.enabled = false;
        }

        public virtual void PopulateWithInfo(WorkingCard card, string setName = "")
        {
            WorkingCard = card;

            libraryCard = WorkingCard.libraryCard;

            nameText.text = libraryCard.name;
            bodyText.text = libraryCard.description;
            costText.text = libraryCard.cost.ToString();

            isNewCard = true;

            manaCost = libraryCard.cost;


            var rarity = Enum.GetName(typeof(Enumerators.CardRarity), WorkingCard.libraryCard.cardRarity);

            backgroundSprite.sprite = Resources.Load<Sprite>(string.Format("Images/Cards/Frames/frame_{0}_{1}", setName, rarity));
            pictureSprite.sprite = Resources.Load<Sprite>(string.Format("Images/Cards/Illustrations/{0}_{1}_{2}", setName.ToLower(), rarity.ToLower(), WorkingCard.libraryCard.picture.ToLower()));


            removeCardParticle = transform.Find("RemoveCardParticle").GetComponent<ParticleSystem>();

            amountText.transform.parent.gameObject.SetActive(false);
        }

        public virtual void PopulateWithLibraryInfo(Card card, string setName = "", int amount = 0)
        {
            libraryCard = card;

            nameText.text = libraryCard.name;
            bodyText.text = libraryCard.description;
            amountText.text = amount.ToString();
            costText.text = libraryCard.cost.ToString();

            manaCost = libraryCard.cost;

            var rarity = Enum.GetName(typeof(Enumerators.CardRarity), card.cardRarity);

            backgroundSprite.sprite = Resources.Load<Sprite>(string.Format("Images/Cards/Frames/frame_{0}_{1}", setName, rarity));

            pictureSprite.sprite = Resources.Load<Sprite>(string.Format("Images/Cards/Illustrations/{0}_{1}_{2}", setName.ToLower(), rarity.ToLower(), card.picture.ToLower()));
        }


        public virtual void UpdateAmount(int amount)
        {
            amountText.text = amount.ToString();
        }

        protected Vector3 positionOnHand;
        protected Vector3 rotationOnHand;
        public virtual void RearrangeHand(Vector3 position, Vector3 rotation)
        {
            positionOnHand = position;
            rotationOnHand = rotation;
            if (!isNewCard)
            {
                UpdatePositionOnHand();
            }
            else if (CurrentTurn != 0)
            {
                cardAnimator.enabled = true;
                cardAnimator.SetTrigger("DeckToHand");

                GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARD_DECK_TO_HAND_SINGLE, Constants.CARDS_MOVE_SOUND_VOLUME, false, false);
            }
            isNewCard = false;
        }

        protected virtual void UpdatePositionOnHand()
        {
            transform.DOMove(positionOnHand, 0.5f);
            transform.DORotate(rotationOnHand, 0.5f);
        }

        public virtual void SetDefaultAnimation(int id)
        {
            cardAnimator.enabled = true;
            cardAnimator.SetTrigger("DeckToHandDefault");

            if (GameClient.Get<IDataManager>().CachedUserLocalData.tutorial)
                cardAnimator.SetFloat("Id", 2);
            else
                cardAnimator.SetFloat("Id", id);

            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARD_DECK_TO_HAND_MULTIPLE, Constants.CARDS_MOVE_SOUND_VOLUME, false, false);
        }

        public virtual void UpdateAnimation(string name)
        {
            switch (name)
            {
                case "DeckToHandEnd":
                    cardAnimator.enabled = false;
                    UpdatePositionOnHand();
                    break;
                default:
                    break;
            }
        }

        public virtual bool CanBePlayed(Player owner)
        {
            if (Constants.DEV_MODE)
                return true;

            return GameClient.Get<IGameplayManager>().GetController<PlayerController>().IsActive;// && owner.manaStat.effectiveValue >= manaCost;
        }

        public virtual bool CanBeBuyed(Player owner)
        {
            if (Constants.DEV_MODE)
                return true;
            return owner.Mana >= manaCost;
        }

        public void IsHighlighted()
        {
            //return glowSprite.enabled;
        }

        public void SetHighlightingEnabled(bool enabled)
        {
            glowSprite.enabled = enabled;
        }
    }
}