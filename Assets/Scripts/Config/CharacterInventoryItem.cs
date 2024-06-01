using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Config
{
    public enum CharacterType
    {
        None = -1,
        Warrior,
        Archer
    }

    public interface ISaveData
    {
    }
    
    [Serializable]
    public class CharacterInventory : ISaveData
    {
        [SerializeField]
        public Dictionary<CharacterType, List<CharacterInventoryItem>> charactersDataList;

        public CharacterInventory()
        {
            charactersDataList = new Dictionary<CharacterType, List<CharacterInventoryItem>>();
        }
    }

    [Serializable]
    public class CharacterInventoryItem
    {
        private CharacterType characterType;
        private int level;
        private bool isAvailable;
        private bool isActive;
        
        public CharacterType CharacterType => characterType;
        public int Level => level;
        public bool IsAvailable => isAvailable;
        public bool IsActive => isActive;

        public CharacterInventoryItem(CharacterType characterType, int level, bool isAvailable, bool isActive)
        {
            this.characterType = characterType;
            this.level = level;
            this.isAvailable = isAvailable;
            this.isActive = isActive;
        }

        public void SetAvailability(bool isAvailable)
        {
            this.isAvailable = isAvailable;
        }
    }
}