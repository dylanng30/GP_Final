using UnityEngine;
using System;

namespace GP_Final.Navigation.Core
{
    public interface ILocalPathfinder
    {
        /// <summary>
        /// Sets the destination for the local pathfinder.
        /// </summary>
        void SetDestination(Vector3 target);

        /// <summary>
        /// Stops the current movement.
        /// </summary>
        void Stop();

        /// <summary>
        /// Event fired when the destination is reached.
        /// </summary>
        event Action OnDestinationReached;
        
        /// <summary>
        /// Check if the pathfinder is currently moving.
        /// </summary>
        bool IsMoving { get; }
    }
}
