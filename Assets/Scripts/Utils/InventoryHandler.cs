using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Config;
using Newtonsoft.Json;
using UnityEngine;

namespace Utils
{
    public class InventoryHandler
    {
        private string path => Application.persistentDataPath + "/characterInventory.json";
        public CharacterInventory characterInventory;

        public CharacterInventory CharacterInventory => characterInventory;

        // todo make a map for all data to read and write from local
        private Dictionary<string, ISaveData> dataMapForLocal;
 
        public void WriteData()
        {
            string json = JsonConvert.SerializeObject(characterInventory);
            File.WriteAllText(path, json);
        }
 
        private void ReadData()
        {
            if (!File.Exists(path))
            {
                characterInventory = new CharacterInventory();
                WriteData();
            }
            
            string json = File.ReadAllText(path);
            characterInventory = JsonConvert.DeserializeObject<CharacterInventory>(json);
        }
        public InventoryHandler()
        {
            ReadData();
        }

        public void AddNewCharacterToInventory(CharacterInventoryItem characterInventoryItem)
        {
            if (characterInventory.charactersDataList.ContainsKey(characterInventoryItem.CharacterType))
            {
                if (!characterInventory.charactersDataList[characterInventoryItem.CharacterType]
                        .Contains(characterInventoryItem))
                    characterInventory.charactersDataList[characterInventoryItem.CharacterType]
                        .Add(characterInventoryItem);
            }
            else
            {
                characterInventory.charactersDataList.Add(characterInventoryItem.CharacterType,
                    new List<CharacterInventoryItem>() { characterInventoryItem });
            }
        }

        public void SetCharacterAvailability(CharacterType characterType, bool isAvailable)
        {
            var values = characterInventory.charactersDataList[characterType]
                .Where(character => character.IsAvailable == !isAvailable && character.IsActive);
            var valuesList = values.ToList();
            
            if (valuesList.Count > 0)
            {
                valuesList[0].SetAvailability(isAvailable);
                EventHandler.RaiseOnCharacterAvailabilityChangedEvent(characterType);
            }
        }

        #region Getters

        public bool IfAnyCharacterAvailable(CharacterType characterType)
        {
            return characterInventory.charactersDataList[characterType].Any(character => character.IsAvailable);
        }

        public bool IfAnyCharacterActive(CharacterType characterType)
        {
            return characterInventory.charactersDataList[characterType].Any(character => character.IsActive);
        }

        public int GetAvailableCount(CharacterType characterType)
        {
            return characterInventory.charactersDataList[characterType].Count(character => character.IsAvailable && character.IsActive);
        }

        public int GetTotalCount(CharacterType characterType)
        {
            return characterInventory.charactersDataList[characterType].Count;
        }

        #endregion
    }
}