using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Game.Core.Characters;
using Game.Core.Managers;
using Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

namespace Game.Core
{
    public enum BattleState
    {
        PreparingPhase,
        Battling,
        Paused,
        BattleOver
    }
    
    public class BattleManager : MonoSingleton<BattleManager>
    {
        [SerializeField] private List<BaseCharacter> friendlyCharacters;
        [SerializeField] private List<BaseCharacter> enemyCharacters;
        
        private Dictionary<BaseCharacter, Queue<BaseCharacter>> triggeredCharactersDict;
        private BattleState battleState = BattleState.PreparingPhase;
        private int enemiesCountAtBattleStart;
        private int enemiesCountForSurrenderCall;
        private int friendlyCountAtBattleStart;
        private int friendlyCountForSurrenderCall;

        private bool surrenderRequestResponded = false;
        private InventoryHandler inventoryHandler;

        public Dictionary<BaseCharacter, Queue<BaseCharacter>> TriggeredCharactersDict
        {
            get => triggeredCharactersDict;
            set { triggeredCharactersDict = value; }
        }

        public BattleState BattleState => battleState;
        public InventoryHandler InventoryHandler => inventoryHandler;

        protected override void Awake()
        {
            base.Awake();
            
            // init configs
            inventoryHandler = new InventoryHandler();
        }

        private void Start()
        {
            foreach (var character in enemyCharacters)
                character.OnCharacterDied += OnCharacterDied;
            foreach (var character in friendlyCharacters)
                character.OnCharacterDied += OnCharacterDied;

            surrenderRequestResponded = false;
            TriggeredCharactersDict = new Dictionary<BaseCharacter, Queue<BaseCharacter>>();

            BattleUIManager.Instance.Initialize();
            LoadoutManager.Instance.Initialize();
        }

        public void AddUnit(BaseCharacter baseCharacter)
        {
            friendlyCharacters.Add(baseCharacter);
            baseCharacter.OnCharacterDied += OnCharacterDied;
            if (battleState == BattleState.Battling)
                baseCharacter.StartBattle();
        }

        public void RemoveUnit(BaseCharacter baseCharacter)
        {
            friendlyCharacters.Remove(baseCharacter);
            baseCharacter.OnCharacterDied -= OnCharacterDied;
        }

        public void OnCharacterDied(BaseCharacter baseCharacter)
        {
            if (baseCharacter.IsFriendly)
                friendlyCharacters.Remove(baseCharacter);
            else
                enemyCharacters.Remove(baseCharacter);

            if(TriggeredCharactersDict.ContainsKey(baseCharacter))
                TriggeredCharactersDict.Remove(baseCharacter);
            
            baseCharacter.OnCharacterDied -= OnCharacterDied;
            Destroy(baseCharacter.gameObject);

            if (friendlyCharacters.Count == 0 || enemyCharacters.Count == 0)
                battleState = BattleState.BattleOver;
            
            
            if(battleState == BattleState.Battling && !surrenderRequestResponded)
                if (enemyCharacters.Count != 0 && enemyCharacters.Count <= enemiesCountForSurrenderCall && friendlyCharacters.Count >= friendlyCountForSurrenderCall)
                {
                    battleState = BattleState.Paused;
                    BattleUIManager.Instance.UpdateHuds();
                }
        }

        public BaseCharacter GetNewTarget(BaseCharacter character)
        {
            float minDistance = float.MaxValue;
            BaseCharacter targetCharacter = null;
            IEnumerable<BaseCharacter> list = character.IsFriendly ? enemyCharacters : friendlyCharacters;
            foreach (var enemy in list)
            {
                /*if(TriggeredCharactersDict != null && TriggeredCharactersDict.ContainsKey(character) && TriggeredCharactersDict[character].Contains(enemy))
                    continue;*/
                
                float dist = Vector3.Distance(character.transform.position, enemy.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    targetCharacter = enemy;
                }
            }
            

            return targetCharacter;
        }

        public BaseCharacter GetNextTriggeredTarget(BaseCharacter character)
        {
            if (triggeredCharactersDict.Count == 0 || !triggeredCharactersDict.ContainsKey(character))
                return null;

            if (triggeredCharactersDict[character].Count == 0)
                return null;

            if (triggeredCharactersDict[character].All(character => character == null))
                return null;

            BaseCharacter targetCharacter = triggeredCharactersDict[character].Aggregate((closest, next) =>
            {
                if (closest == null)
                    return next;
                if (next == null)
                    return closest;
                
                return Vector3.Distance(next.transform.position, character.transform.position) <
                    Vector3.Distance(closest.transform.position, character.transform.position)
                        ? next
                        : closest;
            });
            
            triggeredCharactersDict[character] = new Queue<BaseCharacter>(
                TriggeredCharactersDict[character].Where(character => character != targetCharacter));
            
            if (targetCharacter == null)
                return GetNextTriggeredTarget(character);

            return targetCharacter;
        }
        
        public void StartBattle()
        {
            battleState = BattleState.Battling;
            enemiesCountAtBattleStart = enemyCharacters.Count;
            friendlyCountAtBattleStart = friendlyCharacters.Count;
            enemiesCountForSurrenderCall = enemiesCountAtBattleStart / 3;
            friendlyCountForSurrenderCall = friendlyCountAtBattleStart / 2;
            foreach (var character in enemyCharacters)
                character.StartBattle();
            foreach (var character in friendlyCharacters)
                character.StartBattle();

            LoadoutManager.Instance.HideTileSelectionViews();
        }

        public void HandleSurrender(bool acceptedSurrender)
        {
            surrenderRequestResponded = true;
            if (acceptedSurrender)
            {
                battleState = BattleState.BattleOver;
            }
            else
            {
                battleState = BattleState.Battling;
            }
        }


        [ContextMenu("SaveInventory")]
        public void SaveInventory()
        {
            foreach (var character in friendlyCharacters)
            {
                CharacterInventoryItem characterInventoryItem =
                    new CharacterInventoryItem(character.CharacterType, 1, true, true);
                InventoryHandler.AddNewCharacterToInventory(characterInventoryItem);
            }
            
            InventoryHandler.WriteData();
        }
    }
}