﻿// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/


using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;

namespace LoomNetwork.CZB
{
    public class EnemyThatAttacksBecomeFrozenAbility : AbilityBase
    {
        public int value = 1;

        public EnemyThatAttacksBecomeFrozenAbility(Enumerators.CardKind cardKind, AbilityData ability) : base(cardKind, ability)
        {
        }

        public override void Activate()
        {
            base.Activate();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        protected override void OnInputEndEventHandler()
        {
            base.OnInputEndEventHandler();
        }

        protected override void UnitOnAttackEventHandler(object info, int damage)
        {
            base.UnitOnAttackEventHandler(info, damage);
        }

        protected override void UnitGotDamageEventHandler(object from)
        {
            base.UnitGotDamageEventHandler(from);

            if (abilityCallType != Enumerators.AbilityCallType.AT_DEFENCE)
                return;

            if(from is BoardUnit)
                (from as BoardUnit).Stun(Enumerators.StunType.FREEZE, value);
        }
    }
}
