using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Utils.BehaviourTrees;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Core
{
    public class BaseCharacter: MonoBehaviour
    {
        [SerializeField] protected float movementSpeed;
        [SerializeField] protected float damage = 10;
        [SerializeField] protected float attackDuration = 1;
        [SerializeField] protected float initialHealth = 100;
        [SerializeField] protected float currentHealth;
        [SerializeField] public bool isFriendly = true;
        [SerializeField] private BaseCharacter targetCharacter;
        [SerializeField] private bool startMoving = false;
        [SerializeField] private TMP_Text healthText;

        [SerializeField] private Material enemySkin;
        [SerializeField] private Material playerSkin;
        [SerializeField] private MeshRenderer meshRenderer;

        private Coroutine attackCoroutine;
        private BehaviourTree behaviourTree;

        public event Action<BaseCharacter> OnCharacterDied;

        private void Start()
        {
            GetComponentInChildren<Canvas>().worldCamera = Camera.main;
            healthText.transform.rotation = quaternion.identity;
            currentHealth = initialHealth;
            UpdateHealthText();
            meshRenderer.material = isFriendly ? playerSkin : enemySkin;
        }

        public virtual void StartBattle()
        {
            MoveToTarget moveToTargetLeaf = new MoveToTarget(transform, GetComponent<NavMeshAgent>(),
                targetCharacter ? targetCharacter.transform : null);
            
            #region findNextTargetAndAttackSequenceNode

            // findNextTargetAndAttackSequenceNode
            Sequence findNextTargetAndAttackSequenceNode = new Sequence("findNextTargetAndAttackSequence");
            
            findNextTargetAndAttackSequenceNode.AddChild(new Leaf("findNextTargetWithinRangeLeaf", new ActionStrategy(() =>
            {
                targetCharacter = BattleManager.Instance.GetNextTriggeredTarget(this);
            })));
            findNextTargetAndAttackSequenceNode.AddChild(new Leaf("conditionLeaf", new Condition(() => targetCharacter != null && attackCoroutine == null && GetComponent<NavMeshAgent>().isStopped)));
            findNextTargetAndAttackSequenceNode.AddChild(new Leaf("AttackLeaf", new ActionStrategy(() =>
            {
                moveToTargetLeaf.SetForceStopMovement(true);
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

        protected virtual bool TakeDamage(float damageAmount)
        {
            currentHealth -= damageAmount;
            return currentHealth > 0f;
        }

        protected virtual void SetTarget(BaseCharacter targetCharacter)
        {
            
        }

        protected virtual IEnumerator StartAttacking()
        {
            while (targetCharacter && targetCharacter.TakeDamage(damage))
            {
                GetComponent<Rigidbody>().AddForce(Vector3.up * 10);
                UpdateHealthText();
                yield return new WaitForSeconds(attackDuration);
            }
            
            if(attackCoroutine != null)
                StopCoroutine(attackCoroutine);

            attackCoroutine = null;
        }

        private void UpdateHealthText()
        {
            healthText.text = Mathf.RoundToInt(currentHealth).ToString();
        }

        protected virtual void Update()
        {
            behaviourTree?.Process();
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
    }
}