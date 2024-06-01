using System;
using Config;

namespace Utils
{
    public static class EventHandler
    {
        public static event Action<CharacterType> OnCharacterAvailabilityChanged;

        public static void RaiseOnCharacterAvailabilityChangedEvent(CharacterType characterType)
        {
            OnCharacterAvailabilityChanged?.Invoke(characterType);
        }
    }
}