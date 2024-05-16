using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Core
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance;
        
        [SerializeField] private List<BaseCharacter> friendlyCharacters;
        [SerializeField] private List<BaseCharacter> enemyCharacters;
        private Dictionary<BaseCharacter, Queue<BaseCharacter>> triggeredCharactersDict;

        public Dictionary<BaseCharacter, Queue<BaseCharacter>> TriggeredCharactersDict
        {
            get => triggeredCharactersDict;
            set { triggeredCharactersDict = value; }
        }

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            foreach (var character in enemyCharacters)
                character.OnCharacterDied += OnCharacterDied;
            foreach (var character in friendlyCharacters)
                character.OnCharacterDied += OnCharacterDied;

            TriggeredCharactersDict = new Dictionary<BaseCharacter, Queue<BaseCharacter>>();
        }

        public void AddUnit(BaseCharacter baseCharacter)
        {
            friendlyCharacters.Add(baseCharacter);
            baseCharacter.OnCharacterDied += OnCharacterDied;
        }

        public void RemoveUnit(BaseCharacter baseCharacter)
        {
            friendlyCharacters.Remove(baseCharacter);
            baseCharacter.OnCharacterDied -= OnCharacterDied;
        }

        public void OnCharacterDied(BaseCharacter baseCharacter)
        {
            if (baseCharacter.isFriendly)
                friendlyCharacters.Remove(baseCharacter);
            else
                enemyCharacters.Remove(baseCharacter);

            List<BaseCharacter> keys = new List<BaseCharacter>();
            foreach (var key in TriggeredCharactersDict.Keys)
            {
                keys.Add(key);
            }
            foreach (var key in keys)
            {
                if (key == baseCharacter)
                {
                    TriggeredCharactersDict.Remove(key);
                    continue;
                }
                /*TriggeredCharactersDict[key] =
                    new Queue<BaseCharacter>(
                        TriggeredCharactersDict[key].Where(character => character != baseCharacter));*/
            }
            
            baseCharacter.OnCharacterDied -= OnCharacterDied;
            if(friendlyCharacters.Count == 0 || enemyCharacters.Count == 0)
                //EditorApplication.isPaused = true;
                return;
        }

        public BaseCharacter GetNewTarget(BaseCharacter character)
        {
            float minDistance = float.MaxValue;
            BaseCharacter targetCharacter = null;
            IEnumerable<BaseCharacter> list = character.isFriendly ? enemyCharacters : friendlyCharacters;
            foreach (var enemy in list)
            {
                if(TriggeredCharactersDict != null && TriggeredCharactersDict.ContainsKey(character) && TriggeredCharactersDict[character].Contains(enemy))
                    continue;
                
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

            BaseCharacter targetCharacter = triggeredCharactersDict[character].Dequeue();
            
            if (targetCharacter == null)
                return GetNextTriggeredTarget(character);

            return targetCharacter;
        }

        [ContextMenu("StartBattle")]
        public void StartBattle()
        {
            foreach (var character in enemyCharacters)
                character.StartBattle();
            foreach (var character in friendlyCharacters)
                character.StartBattle();
        }
    }
}