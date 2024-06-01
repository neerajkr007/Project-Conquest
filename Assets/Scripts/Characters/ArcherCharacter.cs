using Game.Core.Views;
using UnityEngine;
using Config;

namespace Game.Core.Characters
{
    public class ArcherCharacter : BaseCharacter
    {
        [SerializeField] private GameObject arrowPrefab;

        private void Awake()
        {
            characterType = CharacterType.Archer;
        }

        protected override bool Attack()
        {
            ShootArrow(damage);
            return false;
        }

        private void ShootArrow(float damageToDeal)
        {
            // implement pooling here and fetch from pool
            GameObject newArrow = Instantiate(arrowPrefab, transform, false);
            newArrow.transform.localPosition = Vector3.zero;
            newArrow.transform.localRotation = Quaternion.identity;
            newArrow.transform.parent = transform.parent;
            
            if(!newArrow.TryGetComponent(out ArcherArrowView arrowView))
                return;
            
            arrowView.Initialize(damageToDeal, IsFriendly, OnArrowHitCallback);
        }

        private void OnArrowHitCallback(BaseCharacter hitCharacter)
        {
            if(hitCharacter.GetHealth() > 0)
                return;
            
            if(hitCharacter)
                hitCharacter.RaiseOnCharacterDiedEvent(hitCharacter);

            if (hitCharacter == targetCharacter)
            {
                // for the player who is attacking
                StopAttacking();
            }
        }
    }
}