using UnityEngine;

namespace Loom.ZombieBattleground.View
{
    /// <summary>
    /// Represent visual part of a component.
    /// </summary>
    public interface IView
    {
        Transform Transform { get; }
    }
}
