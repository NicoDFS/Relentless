using System;
using Loom.ZombieBattleground.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class PastReportActionSmall
    {
        public GameObject SelfObject;

        protected IGameplayManager GameplayManager;

        protected ILoadObjectsManager LoadObjectsManager;

        protected CardsController CardsController;

        protected IUIManager UIManager;

        protected Image PreviewImage;

        protected PastActionsPopup.PastActionParam PastActionReport;

        protected PastActionReportPanel _mainRoot;

        private Vector3 _startPosition;

        private bool _isHold = false;

        private float _minDistance = 30f;

        private bool _interacting = false;

        public PastReportActionSmall(PastActionReportPanel root, GameObject prefab, Transform parent, PastActionsPopup.PastActionParam pastActionParam)
        {
            GameplayManager = GameClient.Get<IGameplayManager>();
            LoadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            CardsController = GameplayManager.GetController<CardsController>();
            UIManager = GameClient.Get<IUIManager>();

            _mainRoot = root;
            PastActionReport = pastActionParam;
            SelfObject = Object.Instantiate(prefab, parent, false);
            SelfObject.transform.SetAsFirstSibling();
            PreviewImage = SelfObject.transform.Find("Image").GetComponent<Image>();

            PreviewImage.sprite = GetPreviewImage();

            OnPastReportActionHandler behaviour = SelfObject.transform.Find("Border").GetComponent<OnPastReportActionHandler>();
            behaviour.PointerDowned += MouseDownHandler;
        }

        public void Update()
        {
            if (_interacting)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!_isHold && _mainRoot.IsDrawing)
                    {
                        UIManager.HidePopup<PastActionsPopup>();
                        _mainRoot.IsDrawing = false;

                        _interacting = false;
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    if (_isHold && _mainRoot.IsDrawing)
                    {
                        UIManager.HidePopup<PastActionsPopup>();
                        _mainRoot.IsDrawing = false;
                        _isHold = false;

                        _interacting = false;
                    }
                    else if (!_isHold && !_mainRoot.IsDrawing)
                    {
                        if (Vector3.Distance(_startPosition, Input.mousePosition) <= _minDistance)
                        {
                            UIManager.DrawPopup<PastActionsPopup>(PastActionReport);
                            _mainRoot.IsDrawing = true;
                        }
                    }
                }
            }
        }

        public void MouseDownHandler(BaseEventData data)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isHold = false;
                _interacting = true;

                if (!_mainRoot.IsDrawing)
                {
                    _startPosition = Input.mousePosition;
                    Helpers.InternalTools.DoActionDelayed(PastActionReportClickCompleted, 0.15f);
                }
            }
        }

        private void PastActionReportClickCompleted()
        {
            if (Input.GetMouseButton(0) && _interacting)
            {
                if (!_mainRoot.IsDrawing)
                {
                    if (Vector3.Distance(_startPosition, Input.mousePosition) <= _minDistance)
                    {
                        UIManager.DrawPopup<PastActionsPopup>(PastActionReport);
                        _mainRoot.IsDrawing = true;
                        _isHold = true;
                    }
                }
            }
        }

        public void Dispose()
        {
            Object.Destroy(SelfObject);
        }

        private Sprite GetPreviewImage()
        {
            Sprite sprite = null;

            switch (PastActionReport.Caller)
            {
                case Player player:
                    sprite = LoadObjectsManager.GetObjectByPath<Sprite>("Images/Heroes/CZB_2D_Hero_Portrait_" +
                        player.SelfHero.HeroElement + "_EXP");
                    break;
                case BoardUnitModel unit:
                    {
                        Enumerators.SetType setType = CardsController.GetSetOfCard(unit.Card.LibraryCard);
                        string rank = unit.Card.LibraryCard.CardRank.ToString().ToLowerInvariant();
                        string picture = unit.Card.LibraryCard.Picture.ToLowerInvariant();

                        string fullPathToPicture = string.Format("Images/Cards/Illustrations/{0}_{1}_{2}", setType.ToString().ToLowerInvariant(), rank, picture);
                        sprite = LoadObjectsManager.GetObjectByPath<Sprite>(fullPathToPicture);
                    }
                    break;
                case BoardCardView card:
                    if (card.PictureSprite && card.PictureSprite != null)
                    {
                        sprite = card.PictureSprite.sprite;
                    }
                    break;
                case BoardSkill skill:
                    sprite = LoadObjectsManager.GetObjectByPath<Sprite>("Images/OverlordAbilitiesIcons/" + skill.Skill.IconPath);
                    break;
                case BoardSpell spell:
                    {
                        Enumerators.SetType setType = CardsController.GetSetOfCard(spell.Card.LibraryCard);
                        string rank = spell.Card.LibraryCard.CardRank.ToString().ToLowerInvariant();
                        string picture = spell.Card.LibraryCard.Picture.ToLowerInvariant();

                        string fullPathToPicture = string.Format("Images/Cards/Illustrations/{0}_{1}_{2}", setType.ToString().ToLowerInvariant(), rank, picture);
                        sprite = LoadObjectsManager.GetObjectByPath<Sprite>(fullPathToPicture);
                    }
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(PastActionReport.Caller), PastActionReport.Caller, null);
            }

            return sprite;
        }
    }
}
