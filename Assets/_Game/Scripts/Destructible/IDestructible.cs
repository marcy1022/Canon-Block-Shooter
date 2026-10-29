using System;
using UnityEngine;

namespace SmashPuzzle.Destructible
{
    public interface IDestructible
    {
        void TakeDamage(float damage);
        bool IsDestroyed { get; }
        event Action<DestructibleObject> OnDestroyed;
    }
}
