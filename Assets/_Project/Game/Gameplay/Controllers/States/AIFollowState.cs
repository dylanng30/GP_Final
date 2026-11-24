using System.Collections.Generic;
using Dylanng;
using Game.Events;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using UnityEngine;

namespace Game.Gameplay.Controllers.States
{
    public class AIFollowState : StateBase
    {
        private readonly IEntityController _entityController;
        private readonly HeroInstance _heroInstance;
        private Transform _targetToFollow;
        private Vector2 _currentMoveVector;
        
        // Steering Follow/Separation Settings
        private const float StoppingDistance = 0.5f; // Stopping distance relative to slot
        private const float SeparationDistance = 1.8f;
        private const float SeparationWeight = 1.5f;
        private const float DeadzoneSqr = 0.01f; // (0.1f * 0.1f)
        private const float MovementLerpSpeed = 5f; // Speed of lerping the move vector for smoothness
        
        // Hysteresis/Stability track for slot assignments
        private static readonly Dictionary<EntityId, int> _lastAssignedSlotIndices = new Dictionary<EntityId, int>();
        
        public AIFollowState(IEntityController entityController, HeroInstance heroInstance)
        {
            _entityController = entityController;
            _heroInstance = heroInstance;
        }

        public override void Enter()
        {
            base.Enter();
            
            EventBus.Subscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);

            RefreshTargetToFollow();
            _currentMoveVector = Vector2.zero;
        }

        public override void Exit()
        {
            EventBus.Unsubscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
            _entityController.StopMove();
            _lastAssignedSlotIndices.Remove(_heroInstance.GetEntityId());
        }

        public override void Update()
        {
            base.Update();
            
            var partyManager = ServiceLocator.Get<PartyCombatManager>();
            if (_heroInstance == partyManager?.GetActiveHero())
            {
                return;
            }

            if (_targetToFollow != null)
            {

                // Get individual slot position instead of active hero center
                Vector3 targetSlotPosition = GetSlotPosition(partyManager);
                float distanceToSlot = Vector3.Distance(_heroInstance.transform.position, targetSlotPosition);
                
                // Calculate follow force towards the slot
                Vector3 followDirection = Vector3.zero;
                if (distanceToSlot > StoppingDistance)
                {
                    followDirection = (targetSlotPosition - _heroInstance.transform.position).normalized;
                }

                // Calculate separation force
                Vector3 separationDirection = Vector3.zero;
                if (partyManager != null && partyManager.ActiveHeroes != null)
                {
                    var activeHeroes = partyManager.ActiveHeroes;
                    int count = activeHeroes.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var otherHero = activeHeroes[i];
                        if (otherHero == null || otherHero == _heroInstance) continue;

                        float distToOther = Vector3.Distance(_heroInstance.transform.position, otherHero.transform.position);
                        if (distToOther < SeparationDistance)
                        {
                            Vector3 pushDir = _heroInstance.transform.position - otherHero.transform.position;
                            pushDir.y = 0;

                            if (pushDir.sqrMagnitude < 0.001f)
                            {
                                // Use instance ID difference to generate a stable, deterministic push angle
                                int idDiff = _heroInstance.GetEntityId().GetHashCode() - otherHero.GetEntityId().GetHashCode();
                                float angle = (idDiff * 45f) % 360f;
                                pushDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                            }
                            else
                            {
                                pushDir.Normalize();
                            }

                            // Linear separation strength based on distance (closer = stronger)
                            float weight = 1.0f - (distToOther / SeparationDistance);
                            separationDirection += pushDir * weight;
                        }
                    }
                }

                // Combine forces
                Vector3 combinedDirection = followDirection + separationDirection * SeparationWeight;

                Vector2 targetMoveVector = Vector2.zero;

                // Move or stop
                if (combinedDirection.sqrMagnitude > DeadzoneSqr)
                {
                    Vector3 finalMoveDirection = combinedDirection.normalized;

                    // Project the world space direction to camera space for HeroInstance's controller compatibility
                    if (Camera.main != null)
                    {
                        Transform camTransform = Camera.main.transform;
                        Vector3 cameraForward = camTransform.forward;
                        Vector3 cameraRight = camTransform.right;

                        cameraForward.y = 0f;
                        cameraRight.y = 0f;
                        cameraForward.Normalize();
                        cameraRight.Normalize();

                        float moveX = Vector3.Dot(finalMoveDirection, cameraRight);
                        float moveY = Vector3.Dot(finalMoveDirection, cameraForward);

                        targetMoveVector = new Vector2(moveX, moveY);
                    }
                    else
                    {
                        targetMoveVector = new Vector2(finalMoveDirection.x, finalMoveDirection.z);
                    }
                }

                // Smoothly interpolate the movement vector
                _currentMoveVector = Vector2.Lerp(_currentMoveVector, targetMoveVector, Time.deltaTime * MovementLerpSpeed);

                if (_currentMoveVector.sqrMagnitude > 0.001f)
                {
                    _entityController.Move(_currentMoveVector);
                }
                else
                {
                    _entityController.StopMove();
                }
            }
        }

        private Vector3 GetSlotPosition(PartyCombatManager partyManager)
        {
            if (_targetToFollow == null || partyManager == null) return _heroInstance.transform.position;

            var activeHeroes = partyManager.ActiveHeroes;
            if (activeHeroes == null) return _targetToFollow.position;

            // 1. Gather all followers (excluding the active leader hero)
            var followers = new List<HeroInstance>();
            int count = activeHeroes.Count;
            for (int i = 0; i < count; i++)
            {
                var hero = activeHeroes[i];
                if (hero == null || hero.transform == _targetToFollow) continue;
                followers.Add(hero);
            }

            if (followers.Count == 0) return _targetToFollow.position;

            // Sort followers by InstanceID to ensure stable, identical order across all calculations
            followers.Sort((a, b) => a.GetEntityId().CompareTo(b.GetEntityId()));

            // 2. Calculate the world space position of each raw slot index
            var slotPositions = new List<Vector3>();
            for (int i = 0; i < followers.Count; i++)
            {
                slotPositions.Add(CalculateSlotPositionWorld(i, followers.Count));
            }

            // 3. Find the best assignment (minimum sum of squared distances with loyalty bonus)
            int n = followers.Count;
            bool[] slotUsed = new bool[n];
            int[] currentAssignment = new int[n];
            int[] bestAssignment = new int[n];
            float minCost = float.MaxValue;

            FindBestAssignment(followers, slotPositions, 0, slotUsed, currentAssignment, ref minCost, bestAssignment);

            // 4. Save assignments for the next frame's hysteresis check
            for (int i = 0; i < n; i++)
            {
                _lastAssignedSlotIndices[followers[i].GetEntityId()] = bestAssignment[i];
            }

            // 5. Retrieve the slot position for the current hero instance
            int myIndexInFollowers = followers.IndexOf(_heroInstance);
            if (myIndexInFollowers >= 0 && myIndexInFollowers < n)
            {
                int assignedSlotIndex = bestAssignment[myIndexInFollowers];
                return slotPositions[assignedSlotIndex];
            }

            return _targetToFollow.position;
        }

        private Vector3 CalculateSlotPositionWorld(int slotIndex, int totalFollowers)
        {
            if (_targetToFollow == null) return Vector3.zero;

            Vector3 leaderForward = _targetToFollow.forward;
            Vector3 leaderRight = _targetToFollow.right;
            leaderForward.y = 0;
            leaderRight.y = 0;
            leaderForward.Normalize();
            leaderRight.Normalize();

            Vector3 offset = Vector3.zero;
            int totalHeroes = totalFollowers + 1; // including active hero

            if (totalHeroes <= 2)
            {
                // Line formation (1 follower): Follow directly behind
                offset = -leaderForward * 2.5f;
            }
            else if (totalHeroes == 3)
            {
                // Equilateral Triangle formation (2 followers)
                float sideLength = 3.0f;
                float height = sideLength * 0.866f; // height of equilateral triangle
                
                if (slotIndex == 0)
                {
                    // Bottom-Left
                    offset = -leaderForward * height - leaderRight * (sideLength * 0.5f);
                }
                else
                {
                    // Bottom-Right
                    offset = -leaderForward * height + leaderRight * (sideLength * 0.5f);
                }
            }
            else if (totalHeroes == 4)
            {
                // Square/Diamond formation (3 followers)
                float sideLength = 2.5f;
                if (slotIndex == 0)
                {
                    // Mid-Left corner
                    offset = -leaderForward * sideLength - leaderRight * sideLength;
                }
                else if (slotIndex == 1)
                {
                    // Mid-Right corner
                    offset = -leaderForward * sideLength + leaderRight * sideLength;
                }
                else
                {
                    // Back corner (completing the square/diamond)
                    offset = -leaderForward * (sideLength * 2.0f);
                }
            }
            else if (totalHeroes == 5)
            {
                // Pentagon formation (4 followers)
                float r = 2.5f;
                if (slotIndex == 0)
                {
                    // Top-Left (wide left, slightly behind)
                    offset = -leaderForward * (0.69f * r) - leaderRight * (0.95f * r);
                }
                else if (slotIndex == 1)
                {
                    // Top-Right (wide right, slightly behind)
                    offset = -leaderForward * (0.69f * r) + leaderRight * (0.95f * r);
                }
                else if (slotIndex == 2)
                {
                    // Bottom-Left (narrow left, further behind)
                    offset = -leaderForward * (1.81f * r) - leaderRight * (0.59f * r);
                }
                else
                {
                    // Bottom-Right (narrow right, further behind)
                    offset = -leaderForward * (1.81f * r) + leaderRight * (0.59f * r);
                }
            }
            else
            {
                // Core Pentagon (first 4 followers) + Outer rows for 6+ heroes
                if (slotIndex < 4)
                {
                    float r = 2.5f;
                    if (slotIndex == 0)
                        offset = -leaderForward * (0.69f * r) - leaderRight * (0.95f * r);
                    else if (slotIndex == 1)
                        offset = -leaderForward * (0.69f * r) + leaderRight * (0.95f * r);
                    else if (slotIndex == 2)
                        offset = -leaderForward * (1.81f * r) - leaderRight * (0.59f * r);
                    else
                        offset = -leaderForward * (1.81f * r) + leaderRight * (0.59f * r);
                }
                else
                {
                    // Outer rows (further back)
                    int outerIndex = slotIndex - 4;
                    float row = (outerIndex / 2) + 1;
                    float side = (outerIndex % 2 == 0) ? -1.0f : 1.0f;
                    offset = -leaderForward * (4.52f + row * 1.5f) + leaderRight * side * (1.2f + row * 0.5f);
                }
            }

            return _targetToFollow.position + offset;
        }

        private static void FindBestAssignment(
            List<HeroInstance> followers, 
            List<Vector3> slotPositions, 
            int currentIndex, 
            bool[] slotUsed, 
            int[] currentAssignment, 
            ref float minCost, 
            int[] bestAssignment)
        {
            int n = followers.Count;
            if (currentIndex == n)
            {
                // Calculate total cost (sum of squared distances)
                float totalCost = 0f;
                for (int i = 0; i < n; i++)
                {
                    float sqrDist = Vector3.SqrMagnitude(followers[i].transform.position - slotPositions[currentAssignment[i]]);
                    
                    // Apply loyalty bonus to prevent jitter/flicker at midpoints
                    EntityId heroID = followers[i].GetEntityId();
                    if (_lastAssignedSlotIndices.TryGetValue(heroID, out int lastSlot) && lastSlot == currentAssignment[i])
                    {
                        sqrDist -= 1.0f; // loyalty bonus (in units of distance squared)
                    }
                    
                    totalCost += sqrDist;
                }
                
                if (totalCost < minCost)
                {
                    minCost = totalCost;
                    System.Array.Copy(currentAssignment, bestAssignment, n);
                }
                return;
            }

            for (int i = 0; i < n; i++)
            {
                if (!slotUsed[i])
                {
                    slotUsed[i] = true;
                    currentAssignment[currentIndex] = i;
                    FindBestAssignment(followers, slotPositions, currentIndex + 1, slotUsed, currentAssignment, ref minCost, bestAssignment);
                    slotUsed[i] = false;
                }
            }
        }

        private void OnActiveHeroChanged(ActiveHeroChangedEvent evt)
        {
            RefreshTargetToFollow();
            _lastAssignedSlotIndices.Clear();
        }

        private void RefreshTargetToFollow()
        {
            _targetToFollow = ServiceLocator.Get<PartyCombatManager>().GetActiveHero()?.transform;
        }
    }
}
