// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/

using System.Collections.Generic;
using System.Linq;
using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;
using UnityEngine;

namespace LoomNetwork.CZB
{
    public class TakeUnitTypeToAllyUnitAbility : AbilityBase
    {
        public Enumerators.CardType unitType;

        public TakeUnitTypeToAllyUnitAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            unitType = ability.targetUnitType;
        }

        public override void Activate()
        {
            base.Activate();

            if (abilityCallType != Enumerators.AbilityCallType.ENTRY)

                return;

            Action();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            List<BoardUnit> allies = new List<BoardUnit>();

            allies = playerCallerOfAbility.BoardCards.Where(unit => (unit != abilityUnitOwner) && !unit.hasFeral && (unit.numTurnsOnBoard == 0)).ToList();

            if (allies.Count > 0)
            {
                int random = Random.Range(0, allies.Count);
                TakeTypeToUnit(allies[random]);
            }
        }

        protected override void OnInputEndEventHandler()
        {
            base.OnInputEndEventHandler();
        }

        private void TakeTypeToUnit(BoardUnit unit)
        {
            if (unit == null)

                return;

            switch (unitType)
            {
                case Enumerators.CardType.HEAVY:
                    unit.SetAsHeavyUnit();
                    break;
                case Enumerators.CardType.FERAL:
                    unit.SetAsFeralUnit();
                    break;
            }
        }
    }
}
