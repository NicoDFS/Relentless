using Loom.ZombieBattleground.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public interface ITutorialManager
    {
        TutorialData CurrentTutorial { get; }
        TutorialStep CurrentTutorialStep { get; }

        bool IsTutorial { get; }

        int TutorialsCount { get; }

        bool PlayerWon { get; set; }

        bool UnfinishedTutorial { get; set; }

        void StartTutorial();

        void SetupTutorialById(int id);

        void StopTutorial(bool isManual = false);

        void ActivateSelectHandPointer(Enumerators.TutorialObjectOwner owner);

        void DeactivateSelectHandPointer(Enumerators.TutorialObjectOwner owner);

        void ReportActivityAction(Enumerators.TutorialActivityAction action, int sender = -1);

        void ReportActivityAction(Enumerators.TutorialActivityAction activityAction, BoardObject sender, string tag = "");

        void ActivateDescriptionTooltipByOwner(Enumerators.TutorialObjectOwner owner, Vector3 position);

        void PlayTutorialSound(string sound, float delay = 0f);

        TutorialDescriptionTooltipItem GetDescriptionTooltip(int id);

        SpecificTurnInfo GetCurrentTurnInfo();
        bool IsCompletedActivitiesForThisTurn();
        string GetCardNameById(int id);

        bool IsButtonBlockedInTutorial(string name);
        bool CheckNextTutorial();

        List<Data.Card> GetSpecificCardsBySet(Enumerators.SetType setType);
        Data.CollectionCardData GetCardData(string id);

        event Action OnMenuStepUpdated;
    }
}
