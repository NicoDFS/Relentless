using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Helpers;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class BlockTakeDamageAbilityView : AbilityViewBase<BlockTakeDamageAbility>
    {
        private BattlegroundController _battlegroundController;
        private AbilitiesController _abilitiesController;

        private CardModel _targetedUnit;

        private GameObject _idleVfxObject;

        public BlockTakeDamageAbilityView(BlockTakeDamageAbility ability) : base(ability)
        {
            _battlegroundController = GameplayManager.GetController<BattlegroundController>();
            _abilitiesController = GameplayManager.GetController<AbilitiesController>();
        }

        protected override void OnAbilityAction(object info = null)
        {
            _targetedUnit = info as CardModel;
            _targetedUnit.IsPlayable = false;
            ActionCompleted();
            Ability.OnDeactivate += () =>
            {
                if (_idleVfxObject != null)
                {
                    Object.Destroy(_idleVfxObject);
                }
            };
        }

        private void ActionCompleted()
        {
            float delayBeforeDestroy = 3f;
            float delayAfter = 0;

            string soundName = string.Empty;
            float soundDelay = 0;

            if (Ability.AbilityData.HasVisualEffectType(Enumerators.VisualEffectType.Impact))
            {
                Vector3 offset;

                offset = Vector3.zero;

                Transform unitTransform = _battlegroundController.GetCardViewByModel<BoardUnitView>(_targetedUnit).Transform;

                Vector3 targetPosition = unitTransform.position;

                VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>(Ability.AbilityData.GetVisualEffectByType(Enumerators.VisualEffectType.Impact).Path);

                Data.AbilityData.VisualEffectInfo idleVfxInfo = Ability.AbilityData.GetVisualEffectByType(Enumerators.VisualEffectType.Moving);
                _idleVfxObject = idleVfxInfo == null ? 
                    null :
                    LoadObjectsManager.GetObjectByPath<GameObject>
                    (
                        Ability.AbilityData.GetVisualEffectByType(Enumerators.VisualEffectType.Moving).Path
                    );                

                AbilityEffectInfoView effectInfo = VfxObject.GetComponent<AbilityEffectInfoView>();
                if (effectInfo != null)
                {
                    delayAfter = effectInfo.delayAfterEffect;
                    delayBeforeDestroy = effectInfo.delayBeforeEffect;
                    offset = effectInfo.offset;
                    soundName = effectInfo.soundName;
                    soundDelay = effectInfo.delayForSound;
                }

                CreateVfx(targetPosition, true, delayBeforeDestroy);
                VfxObject.transform.SetParent(unitTransform, false);
                VfxObject.transform.localPosition = offset;
                
                if(_idleVfxObject != null)
                {
                    _idleVfxObject = Object.Instantiate(_idleVfxObject);
                    _idleVfxObject.transform.SetParent(unitTransform, false);
                    _idleVfxObject.transform.localPosition = offset;
                }
            }

            PlaySound(soundName, soundDelay);

            InternalTools.DoActionDelayed(Ability.InvokeVFXAnimationEnded, delayAfter);
        }

        protected override void CreateVfx(Vector3 pos, bool autoDestroy = false, float duration = 3, bool justPosition = false)
        {
            base.CreateVfx(pos, autoDestroy, duration, justPosition);
        }
    }
}
