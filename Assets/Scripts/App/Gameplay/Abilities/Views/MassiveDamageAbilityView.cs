using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Gameplay;
using Loom.ZombieBattleground.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class MassiveDamageAbilityView : AbilityViewBase<MassiveDamageAbility>
    {
        private List<BoardUnitView> _unitsViews;

        private List<BoardObject> _targets;

        private BattlegroundController _battlegroundController;

        private ICameraManager _cameraManager;

        #region LawnmowerFields

        private GameObject _lineObject;

        private GameObject _cardDissapearingPrefab;
        #endregion

        public MassiveDamageAbilityView(MassiveDamageAbility ability) : base(ability)
        {
            _battlegroundController = GameplayManager.GetController<BattlegroundController>();

            _cameraManager = GameClient.Get<ICameraManager>();
        }

        protected override void OnAbilityAction(object info = null)
        {
            _targets = info as List<BoardObject>;
            float delayBeforeDestroy = 3f;
            float delayAfter = 0;
            Vector3 offset = Vector3.zero;
            bool justPosition = false;
            Enumerators.CardNameOfAbility cardNameOfAbility = Enumerators.CardNameOfAbility.None;

            if (Ability.AbilityData.HasVisualEffectType(Enumerators.VisualEffectType.Impact))
            {
                VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>(Ability.AbilityData.GetVisualEffectByType(Enumerators.VisualEffectType.Impact).Path);
                AbilityEffectInfoView effectInfo = VfxObject.GetComponent<AbilityEffectInfoView>();
                if (effectInfo != null)
                {
                    delayAfter = effectInfo.delayAfterEffect;
                    delayBeforeDestroy = effectInfo.delayBeforeEffect;
                    cardNameOfAbility = effectInfo.cardNameOfAbility;
                    offset = effectInfo.offset;
                    justPosition = true;
                }

                Vector3 targetPosition = Vector3.zero;

                switch (cardNameOfAbility)
                {
                    case Enumerators.CardNameOfAbility.None:
                        {
                            foreach (Enumerators.AbilityTargetType target in Ability.AbilityTargetTypes)
                            {
                                switch (target)
                                {
                                    case Enumerators.AbilityTargetType.OPPONENT_ALL_CARDS:
                                        CustomCreateVfx(offset, true, delayBeforeDestroy, justPosition);
                                        break;
                                    case Enumerators.AbilityTargetType.PLAYER_ALL_CARDS:
                                        foreach (BoardUnitView cardPlayer in Ability.PlayerCallerOfAbility.BoardCards)
                                        {
                                            CreateVfx(cardPlayer.Transform.position, true);
                                        }
                                        break;
                                }
                            }
                        }
                        break;
                    case Enumerators.CardNameOfAbility.Lawnmover:
                        {
                            CreateVfx(targetPosition + offset, true, delayBeforeDestroy, true);
                            VfxObject.transform.position = Ability.PlayerCallerOfAbility.IsLocalPlayer ? Vector3.up * 2.05f : Vector3.up * -1.45f;
                            _lineObject = VfxObject.transform.Find("Lawnmover/BurstToxic").gameObject;
                            _cardDissapearingPrefab = VfxObject.transform.Find("Lawnmover/CardsDissapearing/Tears").gameObject;
                            _unitsViews = new List<BoardUnitView>();
                            foreach (BoardObject boardObject in _targets)
                            {
                                switch (boardObject)
                                {
                                    case BoardUnitModel unit:
                                        _unitsViews.Add(_battlegroundController.GetBoardUnitViewByModel(unit));
                                        break;
                                    default:
                                        break;
                                }
                            }

                            Ability.OnUpdateEvent += OnUpdateEventHandler;
                        }
                        break;
                    default:
                        break;
                }
            }
            InternalTools.DoActionDelayed(Ability.InvokeVFXAnimationEnded, delayAfter);
        }

        private void CustomCreateVfx(Vector3 pos, bool autoDestroy = false, float duration = 3, bool justPosition = false)
        {
            int playerPos = Ability.PlayerCallerOfAbility.IsLocalPlayer ? 1 : -1;

            if (!justPosition)
            {
                pos = Utilites.CastVfxPosition(pos * playerPos);
            }
            else
            {
                pos = pos * playerPos;
            }
            ClearParticles();

            base.CreateVfx(pos, autoDestroy, duration, justPosition);
        }

        #region Lawnmower
        private void OnUpdateEventHandler()
        {
            BoardUnitView unitView;
            for (int i = 0; i < _unitsViews.Count; i++)
            {
                unitView = _unitsViews[i];

                if (unitView.GameObject != null)
                {
                    if (_lineObject.transform.position.x + 1f < unitView.Transform.position.x)
                    {
                        Ability.OneActionComleted(unitView.Model);
                        CreateSubParticle(unitView.Transform.position);
                        _unitsViews.Remove(unitView);
                        _cameraManager.ShakeGameplay(Enumerators.ShakeType.Medium);
                    }
                }
            }

            if (_unitsViews.Count == 0)
            {
                Ability.OnUpdateEvent -= OnUpdateEventHandler;
            }
        }

        private void CreateSubParticle(Vector3 pos, float duration = 3)
        {
            GameObject subObject = Object.Instantiate(_cardDissapearingPrefab);
            subObject.SetActive(true);
            subObject.transform.position = pos;
            ParticlesController.RegisterParticleSystem(subObject, true, duration);
            string clipTitle = "Lawnmover_Mangled_F1_V" + UnityEngine.Random.Range(1, 5).ToString();
            PlaySound(clipTitle, 0);
        }

        protected override void CreateVfx(Vector3 pos, bool autoDestroy = false, float duration = 3, bool justPosition = false)
        {
            base.CreateVfx(pos, autoDestroy, duration, justPosition);
        }
        #endregion
    }
}
