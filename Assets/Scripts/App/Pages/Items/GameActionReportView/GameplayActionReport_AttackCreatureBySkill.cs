﻿// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/



using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using LoomNetwork.CZB.Data;

namespace LoomNetwork.CZB
{
    public class GameplayActionReport_AttackCreatureBySkill : ReportViewBase
    {
        private Player _callerPlayer;
        private HeroSkill _usedSkill;
        private int _skillValue;
        private BoardUnit _skillUsedOnUnit;

        public GameplayActionReport_AttackCreatureBySkill(GameObject prefab, Transform parent, GameActionReport gameAction) : base(prefab, parent, gameAction) { }

        public override void SetInfo()
        {
            base.SetInfo();

            _callerPlayer = gameAction.parameters[0] as Player;
            _usedSkill = gameAction.parameters[1] as HeroSkill;
            _skillValue = (int)gameAction.parameters[2];
            _skillUsedOnUnit = gameAction.parameters[3] as BoardUnit;

            previewImage.sprite = _skillUsedOnUnit.sprite;
        }

        public override void OnPointerEnterEventHandler(PointerEventData obj)
        {
            base.OnPointerEnterEventHandler(obj);
        }

        public override void OnPointerExitEventHandler(PointerEventData obj)
        {
            base.OnPointerExitEventHandler(obj);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

    }
}
