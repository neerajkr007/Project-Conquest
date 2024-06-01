using System;
using Config;
using Game.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EventHandler = Utils.EventHandler;

namespace Game.Core.Views
{
    public class LoadoutOptionView : MonoBehaviour
    {
        [Header("Asset Refs")] 
        [SerializeField]
        private TMP_Text characterTypeText;
        [SerializeField]
        private TMP_Text countDataText;
        [SerializeField] 
        private Button button;
        
        private CharacterType characterType = CharacterType.None;
        private int availableCount => BattleManager.Instance.InventoryHandler.GetAvailableCount(characterType);
        private int totalCount => BattleManager.Instance.InventoryHandler.GetTotalCount(characterType);

        // public ref
        public CharacterType CharacterType => characterType;

        public void Initialize(CharacterType characterType)
        {
            this.characterType = characterType;
            
            UpdateCharacterTypeText(characterType.ToString());
            EventHandler.RaiseOnCharacterAvailabilityChangedEvent(characterType);
        }

        private void Awake()
        {
            button.onClick.AddListener(OptionSelected);
            EventHandler.OnCharacterAvailabilityChanged += UpdateCountsText;
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OptionSelected);
            EventHandler.OnCharacterAvailabilityChanged -= UpdateCountsText;
        }

        private void UpdateCharacterTypeText(string characterType)
        {
            characterTypeText.text = characterType;
        }

        private void UpdateCountsText(CharacterType characterType)
        {
            if(characterType != this.characterType)
                return;
            
            countDataText.text = $"{availableCount}/{totalCount}";
        }
        
        private void OptionSelected()
        {
            LoadoutManager.Instance.OnLoadoutOptionTapped(characterType);
        }
    }
}