using Dylanng.Core.Base;
using UnityEngine;

namespace Dylanng.Core.Commands
{
    public class MoveCommand : CommandBase
    {
        private Transform _actor;
        private Vector3 _destination;
        private Vector3 _previousPosition;

        public MoveCommand(Transform actor, Vector3 destination)
        {
            _actor = actor;
            _destination = destination;
        }

        public override void Execute()
        {
            _previousPosition = _actor.position;
            // logic di chuyển (ví dụ set destination cho NavMeshAgent hoặc nội suy)
            _actor.position = _destination; 
        }

        public override void Undo()
        {
            _actor.position = _previousPosition;
        }
    }
}
