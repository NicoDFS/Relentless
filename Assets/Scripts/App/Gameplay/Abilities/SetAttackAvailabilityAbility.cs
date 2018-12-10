using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;

namespace Loom.ZombieBattleground
{
    public class SetAttackAvailabilityAbility : AbilityBase
    {
        public SetAttackAvailabilityAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
        }

        public override void Activate()
        {
            base.Activate();

            if (AbilityCallType != Enumerators.AbilityCallType.ENTRY)
                return;

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>(), AbilityData.AbilityType, Protobuf.AffectObjectType.Types.Enum.Character);

            SetAttackAvailability(AbilityUnitOwner);
        }

        private void SetAttackAvailability(BoardUnitModel boardUnit)
        {
            if (boardUnit == null)
                return;

            if (AbilityTargetTypes.Count > 0)
            {
                boardUnit.AttackTargetsAvailability.Clear();

                foreach(Enumerators.AbilityTargetType targetType in AbilityTargetTypes)
                {
                    switch(targetType)
                    {
                        case Enumerators.AbilityTargetType.OPPONENT:
                            boardUnit.AttackTargetsAvailability.Add(Enumerators.SkillTargetType.OPPONENT);
                            break;
                        case Enumerators.AbilityTargetType.OPPONENT_CARD:
                        case Enumerators.AbilityTargetType.OPPONENT_ALL_CARDS:
                            boardUnit.AttackTargetsAvailability.Add(Enumerators.SkillTargetType.OPPONENT_CARD);
                            break;
                    }
                }
            }
            else
            {
                boardUnit.CanAttackByDefault = false;
            }
        }
    }
}
