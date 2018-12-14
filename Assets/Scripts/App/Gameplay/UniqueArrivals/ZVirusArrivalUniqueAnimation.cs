using System.Collections.Generic;
using UnityEngine;
using System;
using Loom.ZombieBattleground.Helpers;
using Object = UnityEngine.Object;
using DG.Tweening;

namespace Loom.ZombieBattleground
{
    public class ZVirusArrivalUniqueAnimation : UniqueAnimation
    {
        public override void Play(BoardObject boardObject, Action startGeneralArrivalCallback, Action endArrivalCallback)
        {
            startGeneralArrivalCallback?.Invoke();

            IsPlaying = true;

            Vector3 offset = new Vector3(-5.2f, 0.26f, 0f);

            const float delayBeforeSpawn = 0.7f;
            const float delayBeforeDestroyVFX = 5f;

            BoardUnitView unitView = BattlegroundController.GetBoardUnitViewByModel(boardObject as BoardUnitModel);

            unitView.GameObject.SetActive(false);

            InternalTools.DoActionDelayed(() =>
            {
                GameObject animationVFX = Object.Instantiate(LoadObjectsManager.GetObjectByPath<GameObject>(
                                                            "Prefabs/VFX/UniqueArrivalAnimations/ZVirus"));

                PlaySound("Z_Virus");

                animationVFX.transform.position = unitView.PositionOfBoard + offset;

                unitView.Transform.SetParent(animationVFX.transform.Find("ZVirusCardPH"), true);
                unitView.GameObject.SetActive(true);

                InternalTools.DoActionDelayed(() =>
                {
                    unitView.Transform.SetParent(null, true);
                    Object.Destroy(animationVFX);

                    endArrivalCallback?.Invoke();

                    if (unitView.Model.OwnerPlayer.IsLocalPlayer)
                    {
                        BattlegroundController.UpdatePositionOfBoardUnitsOfPlayer(unitView.Model.OwnerPlayer.BoardCards);
                    }
                    else
                    {
                        BattlegroundController.UpdatePositionOfBoardUnitsOfOpponent();
                    }

                    IsPlaying = false;
                }, delayBeforeDestroyVFX);
            }, delayBeforeSpawn);
        }
    }
}
