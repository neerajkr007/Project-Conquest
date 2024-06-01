using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using Game.Core.Utils.BehaviourTrees;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Core.Characters
{
    public class BaseCharacter: MonoBehaviour
    {
        [Header("Attributes")]
        [SerializeField] protected float movementSpeed = 3.5f;
        [SerializeField] protected float damage = 10;
        [SerializeField] protected float attackDuration = 1;
        [SerializeField] protected float initialHealth = 100;
        [SerializeField] protected float triggerRadius = 1.3f;
        [SerializeField] private bool isFriendly = true;
        
        [Header("Asset Refs")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Material enemySkin;
        [SerializeField] private Material playerSkin;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] protected NavMeshAgent navMeshAgent;
        [SerializeField] protected SphereCollider triggerDetectionCollider;

        protected float currentHealth;
        protected BehaviourTree behaviourTree;
        protected BaseCharacter targetCharacter;
        protected Coroutine attackCoroutine;
        protected MoveToTarget moveToTargetLeaf;
        protected CharacterType characterType;
        
        public event Action<BaseCharacter> OnCharacterDied;
        
        // public accessors
        public float TriggerRadius => triggerRadius;
        public bool IsFriendly => isFriendly;
        public CharacterType CharacterType => characterType;

        private void Start()
        {
            GetComponentInChildren<Canvas>().worldCamera = Camera.main;
            healthText.transform.rotation = quaternion.identity;
            currentHealth = initialHealth;
            UpdateHealthText();
            
            // skin
            meshRenderer.material = isFriendly ? playerSkin : enemySkin;
            
            // navMeshAgent
            navMeshAgent.speed = movementSpeed;
            navMeshAgent.isStopped = true;
            
            // collider setup
            triggerDetectionCollider.radius = triggerRadius;
            
            // event subs
            OnCharacterDied += CharacterDied;
        }

        protected virtual void CharacterDied(BaseCharacter baseCharacter)
        {
            // called when this character dies
            StopAttacking();
            foreach (var kvp in BattleManager.Instance.TriggeredCharactersDict)
            {
                if (kvp.Key == this)
                    continue;

                if(kvp.Key.targetCharacter == this)
                    kvp.Key.StopAttacking();
            }
            
            OnCharacterDied -= CharacterDied;
        }

        public void StartBattle()
        {
            triggerDetectionCollider.enabled = true;
            SetupBehaviourTree();
        }
        
        public virtual void SetupBehaviourTree()
        {
            moveToTargetLeaf = new MoveToTarget(transform, navMeshAgent,
                targetCharacter ? targetCharacter.transform : null, GetMinDistanceToStopMoving());
            
            #region findNextTargetAndAttackSequenceNode

            // findNextTargetAndAttackSequenceNode
            Sequence findNextTargetAndAttackSequenceNode = new Sequence("findNextTargetAndAttackSequence");
            
            findNextTargetAndAttackSequenceNode.AddChild(new Leaf("findNextTargetWithinRangeLeaf", new ActionStrategy(() =>
            {
                targetCharacter = BattleManager.Instance.GetNextTriggeredTarget(this);
            })));
            findNextTargetAndAttackSequenceNode.AddChild(new Leaf("conditionLeaf", new Condition(() => targetCharacter != null && attackCoroutine == null && navMeshAgent.isStopped)));
            findNextTargetAndAttackSequenceNode.AddChild(new Leaf("CheckDistanceLeaf",
                new FuncStrategyWithNodeStatus(() =>
                {
                    var dist = Vector3.Distance(targetCharacter.transform.position, transform.position);
                    var minDist = GetMinDistanceToStopMoving();
                    if ( dist > minDist)
                    {
                        BattleManager.Instance.TriggeredCharactersDict[this].Enqueue(targetCharacter);
                        targetCharacter = null;
                        return Node.Status.Failure;
                    }

                    return Node.Status.Success;
                })));
            findNextTargetAndAttackSequenceNode.AddChild(new Leaf("AttackLeaf", new ActionStrategy(() =>
            {
                attackCoroutine = StartCoroutine(StartAttacking());
            })));

            #endregion
            
            PrioritySelector prioritySelectorNode = new PrioritySelector("prioritySelectorNode");

            Sequence moveToTargetSequence = new Sequence("moveToTargetSequence", 80);
            moveToTargetSequence.AddChild(new Leaf("conditionLeaf", new Condition(() => targetCharacter != null && attackCoroutine == null)));
            moveToTargetSequence.AddChild(new Leaf("UpdateTargetLeaf",
                new ActionStrategy(() =>
                    {
                        moveToTargetLeaf.SetTargetDynamic(targetCharacter ? targetCharacter.transform : null);
                        moveToTargetLeaf.SetMinDistanceToStop(GetMinDistanceToStopMoving());
                        moveToTargetLeaf.SetForceStopMovement(false);
                    }
                )));
            moveToTargetSequence.AddChild(new Leaf("MoveToTarget", moveToTargetLeaf));
            moveToTargetSequence.AddChild(findNextTargetAndAttackSequenceNode);

            Sequence attackTargetSequence = new Sequence("attackTargetSequence", 100);
            attackTargetSequence.AddChild(new Leaf("conditionLeaf", new Condition(() => targetCharacter == null)));
            attackTargetSequence.AddChild(findNextTargetAndAttackSequenceNode);

            Sequence findNextTargetToMoveToSequence = new Sequence("findNextTargetToMoveToSequence", 90);
            findNextTargetToMoveToSequence.AddChild(new Leaf("conditionLeaf", new Condition(() => targetCharacter == null && attackCoroutine == null)));
            findNextTargetToMoveToSequence.AddChild(new Leaf("FindNextTargetToMoveToLeaf", new ActionStrategy(() =>
            {
                targetCharacter = BattleManager.Instance.GetNewTarget(this);
            })));
            // add code here for situation where no enemies alive
            
            prioritySelectorNode.AddChild(attackTargetSequence);
            prioritySelectorNode.AddChild(moveToTargetSequence);
            prioritySelectorNode.AddChild(findNextTargetToMoveToSequence);
            
            behaviourTree = new BehaviourTree("Behaviour Tree", Policies.RunForever);
            behaviourTree.AddChild(prioritySelectorNode);
        }

        public virtual void TakeDamage(float damageAmount)
        {
            currentHealth -= damageAmount;
            UpdateHealthText();
        }

        public float GetHealth()
        {
            return currentHealth;
        }

        protected virtual void SetTarget(BaseCharacter targetCharacter)
        {
            
        }
        
        protected IEnumerator StartAttacking()
        {
            while (targetCharacter && targetCharacter.GetHealth() > 0)
            {
                // check if the battle is paused by the battle manager
                yield return new WaitUntil(() => BattleManager.Instance.BattleState != BattleState.Paused);
                
                // do attack - can be different kind of attacks here
                // for eg. this is where u shoot an arrow for the archer character, it's not an instant damage but
                // rather when it hit the target
                // so remember to override the attack method
                if(Attack())
                    break;
                
                yield return new WaitForSeconds(attackDuration);
            }
            
            if(targetCharacter)
                targetCharacter.RaiseOnCharacterDiedEvent(targetCharacter);

            // for the player who is attacking
            StopAttacking();
        }

        // returns a flag deciding if the while loop in StartAttacking coroutine should break or not 
        protected virtual bool Attack()
        {
            return false;
        }

        protected void StopAttacking()
        {
            if(attackCoroutine != null)
                StopCoroutine(attackCoroutine);

            attackCoroutine = null;
            
            moveToTargetLeaf.SetForceStopMovement(true);
        }

        private void UpdateHealthText()
        {
            healthText.text = Mathf.RoundToInt(currentHealth).ToString();
        }

        protected virtual void Update()
        {
            if (BattleManager.Instance.BattleState == BattleState.Paused ||
                BattleManager.Instance.BattleState == BattleState.BattleOver)
                return;

            behaviourTree?.Process();
            if (targetCharacter)
            {
                /*Vector3 direction = (targetCharacter.transform.position - transform.position).normalized;
                Quaternion toRotation = Quaternion.FromToRotation(transform.forward, direction);
                transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, 0.1f * Time.deltaTime);*/
                transform.LookAt(new Vector3(targetCharacter.transform.position.x, transform.position.y, targetCharacter.transform.position.z));
            }
            
        }

        private void LateUpdate()
        {
            healthText.transform.LookAt(healthText.transform.position + Camera.main.transform.rotation*Vector3.forward, Camera.main.transform.rotation*Vector3.up);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if(!other.transform.TryGetComponent(out BaseCharacter enemyCharacter))
                return;
            
            if(enemyCharacter.isFriendly == isFriendly)
                return;

            if (BattleManager.Instance.TriggeredCharactersDict.ContainsKey(this))
                BattleManager.Instance.TriggeredCharactersDict[this].Enqueue(enemyCharacter);
            else
                BattleManager.Instance.TriggeredCharactersDict.Add(this,
                    new Queue<BaseCharacter>(new List<BaseCharacter>() { enemyCharacter }));
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if(!other.transform.TryGetComponent(out BaseCharacter enemyCharacter))
                return;
            
            if(enemyCharacter.isFriendly && isFriendly)
                return;

            if (BattleManager.Instance.TriggeredCharactersDict.ContainsKey(this))
                BattleManager.Instance.TriggeredCharactersDict[this] = new Queue<BaseCharacter>(BattleManager.Instance
                    .TriggeredCharactersDict[this].Where(target => target != enemyCharacter));
        }

        protected float GetMinDistanceToStopMoving()
        {
            float targetTriggerRadius = targetCharacter ? targetCharacter.TriggerRadius : 0f;
            return triggerRadius + (triggerRadius <= targetTriggerRadius ? triggerRadius : targetTriggerRadius);
        }

        public void RaiseOnCharacterDiedEvent(BaseCharacter baseCharacter)
        {
            OnCharacterDied?.Invoke(baseCharacter);
        }
    }
}