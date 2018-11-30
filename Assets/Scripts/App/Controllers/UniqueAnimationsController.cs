using System.Collections.Generic;
using UnityEngine;
using System;
using Loom.ZombieBattleground.Common;

namespace Loom.ZombieBattleground
{
    public class UniqueAnimationsController : IController
    {
        public void Dispose()
        {
        }

        public void Init()
        {
        }

        public void ResetAll()
        {
        }

        public void Update()
        {
        }

        public bool HasUniqueAnimation(WorkingCard card)
        {
            return card.LibraryCard.UniqueAnimationType != Enumerators.UniqueAnimationType.None;
        }

        public void PlayUniqueArrivalAnimation(BoardObject boardObject, WorkingCard card, Action startGeneralArrivalCallback)
        {
            UniqueAnimation animation = GetUniqueAnimationByType(card.LibraryCard.UniqueAnimationType);
            animation.Play(boardObject, startGeneralArrivalCallback);
        }

        private UniqueAnimation GetUniqueAnimationByType(Enumerators.UniqueAnimationType uniqueAnimationType)
        {
            UniqueAnimation uniqueAnimation;

            switch (uniqueAnimationType)
            {
                case Enumerators.UniqueAnimationType.ShammannArrival:
                    uniqueAnimation = new ShammannArrivalUniqueAnimation();
                    break;
                case Enumerators.UniqueAnimationType.ZVirusArrival:
                    uniqueAnimation = new ZVirusArrivalUniqueAnimation();
                    break;
                case Enumerators.UniqueAnimationType.CerberusArrival:
                    uniqueAnimation = new CerberusArrivalUniqueAnimation();
                    break;
                case Enumerators.UniqueAnimationType.TzunamyArrival:
                    uniqueAnimation = new TzunamyArrivalUniqueAnimation();
                    break;
                case Enumerators.UniqueAnimationType.ChernoBillArrival:
                    uniqueAnimation = new ChernoBillArrivalUniqueAnimation();
                    break;
                default:
                    throw new NotImplementedException(nameof(uniqueAnimationType) + " not implemented yet");
            }

            return uniqueAnimation;
        }
    }
}
