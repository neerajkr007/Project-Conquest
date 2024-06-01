using Config;

namespace Game.Core.Characters
{
    public class WarriorCharacter : BaseCharacter
    {
        private void Awake()
        {
            characterType = CharacterType.Warrior;
        }
        
        protected override bool Attack()
        {
            targetCharacter.TakeDamage(damage);

            if (targetCharacter.GetHealth() <= 0)
                return true;

            return false;
        }
    }
}