using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Core.Utils.BehaviourTrees
{
    public interface IStrategy
    {
        Node.Status Process();

        void Reset()
        {
            // Noop
        }
    }

    public class ActionStrategy : IStrategy
    {
        private readonly Action doSomething;

        public ActionStrategy(Action doSomething)
        {
            this.doSomething = doSomething;
        }

        public Node.Status Process()
        {
            doSomething();
            return Node.Status.Success;
        }
    }

    public class FuncStrategyWithNodeStatus : IStrategy
    {
        private readonly Func<Node.Status> doSomething;

        public FuncStrategyWithNodeStatus(Func<Node.Status> doSomething)
        {
            this.doSomething = doSomething;
        }

        public Node.Status Process()
        {
            return doSomething.Invoke();
        }
    }

    public class Condition : IStrategy
    {
        private readonly Func<bool> predicate;

        public Condition(Func<bool> predicate)
        {
            this.predicate = predicate;
        }

        public Node.Status Process()
        {
            return predicate() ? Node.Status.Success : Node.Status.Failure;
        }
    }

    public class PatrolStrategy : IStrategy
    {
        private readonly NavMeshAgent agent;
        private readonly Transform entity;
        private readonly List<Transform> patrolPoints;
        private readonly float patrolSpeed;
        private int currentIndex;
        private bool isPathCalculated;

        public PatrolStrategy(Transform entity, NavMeshAgent agent, List<Transform> patrolPoints,
            float patrolSpeed = 2f)
        {
            this.entity = entity;
            this.agent = agent;
            this.patrolPoints = patrolPoints;
            this.patrolSpeed = patrolSpeed;
        }

        public Node.Status Process()
        {
            if (currentIndex == patrolPoints.Count) return Node.Status.Success;

            var target = patrolPoints[currentIndex];
            agent.SetDestination(target.position);
            entity.LookAt(new Vector3(target.position.x, entity.position.y, target.position.z));

            if (isPathCalculated && agent.remainingDistance < 0.1f)
            {
                currentIndex++;
                isPathCalculated = false;
            }

            if (agent.pathPending) isPathCalculated = true;

            return Node.Status.Running;
        }

        public void Reset()
        {
            currentIndex = 0;
        }
    }

    public class MoveToTarget : IStrategy
    {
        private readonly NavMeshAgent agent;
        private readonly Transform entity;
        private Transform target;
        private bool isPathCalculated;
        private float minDistToStop;
        private bool forceStopMovement;

        public MoveToTarget(Transform entity, NavMeshAgent agent, Transform target, float minDistToStop)
        {
            this.entity = entity;
            this.agent = agent;
            this.target = target;
            forceStopMovement = false;
        }

        public void SetTargetDynamic(Transform target)
        {
            this.target = target;
        }

        public void SetMinDistanceToStop(float minDistToStop)
        {
            this.minDistToStop = minDistToStop;
        }

        public void SetForceStopMovement(bool forceStopMovement)
        {
            this.forceStopMovement = forceStopMovement;
        }

        public Node.Status Process()
        {
            if (target == null)
                return Node.Status.Failure;
            
            var dist = Vector3.Distance(entity.position, target.position);
            if (forceStopMovement || dist < minDistToStop)
            {
                agent.isStopped = true;
                return Node.Status.Success;
            }

            agent.isStopped = false;
            
            if (BattleManager.Instance.BattleState == BattleState.Paused)
                agent.SetDestination(entity.position);
            
            agent.SetDestination(target.position);

            if (agent.pathPending) 
                isPathCalculated = true;
            
            return Node.Status.Running;
        }

        public void Reset()
        {
            isPathCalculated = false;
        }
    }
}