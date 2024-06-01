using System;
using Game.Core.Characters;
using UnityEngine;

namespace Game.Core.Views
{
    public class ArcherArrowView : MonoBehaviour
    {
        private bool isFriendlyArrow = false;
        private float damageToDeal = 10f;
        private Action<BaseCharacter> onHitCallback;
        private float maxDistance = 50f;
        private float travelSpeed = 10f;
        
        private Vector3 destination;
        private Vector3 direction;
        private float distanceTravelled = 0f;
        private bool ifArrowShot = false;
        private Vector3 startPosition;
        private bool destinationReached = false;
        
        public void Initialize(float damageToDeal, bool isFriendly, Action<BaseCharacter> onHitCallback)
        {
            this.isFriendlyArrow = isFriendly;
            this.damageToDeal = damageToDeal;
            this.onHitCallback = onHitCallback;

            SetupTargetValues();
        } 
        void SetupTargetValues() 
        {
            direction = transform.forward;
            startPosition = transform.position;
            destination = startPosition + direction * maxDistance;
            ifArrowShot = true;
            distanceTravelled = 0f;
        }
   
        
        void Update () 
        {
            if(!ifArrowShot || BattleManager.Instance.BattleState == BattleState.Paused)
                return;
            
            if (distanceTravelled <= maxDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, travelSpeed * Time.deltaTime);
                distanceTravelled += travelSpeed * Time.deltaTime;
            }
            else
                DestroyArrow();
        }

        private void OnTriggerEnter(Collider other)
        {
            if(!other.transform.parent.TryGetComponent(out BaseCharacter enemyCharacter))
                return;
            
            if(enemyCharacter.IsFriendly == isFriendlyArrow)
                return;
            
            enemyCharacter.TakeDamage(damageToDeal);
            onHitCallback?.Invoke(enemyCharacter);
            DestroyArrow();
        }

        private void DestroyArrow()
        {
            ifArrowShot = false;
            Destroy(gameObject);
        }
    }
}