using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Helpers;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class DestroyUnitByCostAbilityView : AbilityViewBase<DestroyUnitByCostAbility>
    {
        private BattlegroundController _battlegroundController;


        public DestroyUnitByCostAbilityView(DestroyUnitByCostAbility ability) : base(ability)
        {
            _battlegroundController = GameplayManager.GetController<BattlegroundController>();
        }

        protected override void OnAbilityAction(object info = null)
        {
            BoardUnitModel unit = info as BoardUnitModel;

            float delayBeforeDestroy = 3f;
            float delayAfter = 0;
            float delayChangeState = 0;
            Vector3 offset = Vector3.zero;

            if (Ability.AbilityData.HasVisualEffectType(Enumerators.VisualEffectType.Impact))
            {
                Vector3 targetPosition = _battlegroundController.GetBoardUnitViewByModel(unit).Transform.position;

                VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>(Ability.AbilityData.GetVisualEffectByType(Enumerators.VisualEffectType.Impact).Path);

                AbilityEffectInfoView effectInfo = VfxObject.GetComponent<AbilityEffectInfoView>();
                if (effectInfo != null)
                {
                    delayAfter = effectInfo.delayAfterEffect;
                    delayBeforeDestroy = effectInfo.delayBeforeEffect;
                    delayChangeState = effectInfo.delayForChangeState;
                    offset = effectInfo.offset;
                }

                InternalTools.DoActionDelayed(() =>
                {
                    _battlegroundController.GetBoardUnitViewByModel(unit).ChangeModelVisibility(false);
                }, delayChangeState);

                CreateVfx(targetPosition, true, delayBeforeDestroy, true);
            }

            InternalTools.DoActionDelayed(Ability.InvokeVFXAnimationEnded, delayAfter);
        }

        protected override void CreateVfx(Vector3 pos, bool autoDestroy = false, float duration = 3, bool justPosition = false)
        {
            base.CreateVfx(pos, autoDestroy, duration, justPosition);
        }
    }
}
