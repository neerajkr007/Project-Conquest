using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Game.Core.Characters;
using Game.Core.Views;
using Unity.Mathematics;
using UnityEngine;
using Utils;

namespace Game.Core.Managers
{
    public class LoadoutManager : MonoSingleton<LoadoutManager>
    {
        [SerializeField] private GameObject WarriorCharacterPrefab;
        [SerializeField] private GameObject ArcherCharacterPrefab;
        [SerializeField] private GameObject CharacterTypeSelectionCardPrefab;

        [SerializeField] private Transform soldiersParent;
        [SerializeField] private GameObject loadoutGO;
        [SerializeField] private Transform characterTypeSelectionCardParent;
        
        private LoadoutSelectionTileView selectedTileView;
        private Dictionary<CharacterType, LoadoutOptionView> characterTypeSelectionCardsMap;

        public void Initialize()
        {
            characterTypeSelectionCardsMap = new Dictionary<CharacterType, LoadoutOptionView>();
            SetupCharacterTypeSelectionCards();
            loadoutGO.SetActive(false);
        }

        private void SetupCharacterTypeSelectionCards()
        {
            var characterInventoryData = BattleManager.Instance.InventoryHandler.CharacterInventory.charactersDataList;

            foreach (CharacterType characterType in characterInventoryData.Keys)
            {
                var newCardGO = Instantiate(CharacterTypeSelectionCardPrefab,
                    characterTypeSelectionCardParent, false);

                if (!newCardGO.TryGetComponent(out LoadoutOptionView loadoutOptionView))
                    return;

                loadoutOptionView.Initialize(characterType);

                characterTypeSelectionCardsMap.Add(characterType, loadoutOptionView);
            }
        }

        public void OnLoadoutSelectionTileTapped(LoadoutSelectionTileView loadoutSelectionTileView)
        {
            if(selectedTileView == loadoutSelectionTileView)
                return;
            
            // reset previously selected tile if any
            selectedTileView?.SetTileSelectedState(false);
            
            selectedTileView = loadoutSelectionTileView;
            selectedTileView.SetTileSelectedState(true);
            
            if(loadoutGO.activeInHierarchy)
                return;
            
            loadoutGO.SetActive(true);
        }

        public void HideTileSelectionViews()
        {
            selectedTileView?.SetTileSelectedState(false);
            var selectionTileViews = FindObjectsOfType<LoadoutSelectionTileView>();
            foreach (var selectionTile in selectionTileViews)
            {
                selectionTile.gameObject.SetActive(false);
            }
        }

        public void OnLoadoutOptionTapped(CharacterType characterType)
        {
            if (selectedTileView.SelectedUnit && selectedTileView.SelectedUnit.CharacterType == characterType)
                return;

            if (!BattleManager.Instance.InventoryHandler.IfAnyCharacterAvailable(characterType) ||
                !BattleManager.Instance.InventoryHandler.IfAnyCharacterActive(characterType))
                return;
            
            GameObject characterPrefab = null;
            LoadoutOptionView loadoutOptionView = null;

            switch (characterType)
            {
                case CharacterType.None:
                    if(selectedTileView.SelectedUnit == null)
                        return;
                    
                    BattleManager.Instance.RemoveUnit(selectedTileView.SelectedUnit);
                    
                    if(characterTypeSelectionCardsMap.TryGetValue(selectedTileView.SelectedUnit.CharacterType, out loadoutOptionView))
                    {
                        BattleManager.Instance.InventoryHandler.SetCharacterAvailability(selectedTileView.SelectedUnit.CharacterType, true);
                    }
                    
                    selectedTileView.SetSelectedUnit(null);
                    return;
                case CharacterType.Archer:
                    characterPrefab = ArcherCharacterPrefab;
                    break;
                case CharacterType.Warrior:
                    characterPrefab = WarriorCharacterPrefab;
                    break;
                default:
                    characterPrefab = WarriorCharacterPrefab;
                    break;
            }

            if(characterTypeSelectionCardsMap.TryGetValue(characterType, out loadoutOptionView))
            {
                BattleManager.Instance.InventoryHandler.SetCharacterAvailability(characterType, false);
                
                if (selectedTileView.SelectedUnit && selectedTileView.SelectedUnit.CharacterType != characterType)
                {
                    if(characterTypeSelectionCardsMap.TryGetValue(selectedTileView.SelectedUnit.CharacterType, out loadoutOptionView))
                    {
                        BattleManager.Instance.InventoryHandler.SetCharacterAvailability(selectedTileView.SelectedUnit.CharacterType, true);
                    }
                }
            }

            GameObject newCharacterGO = Instantiate(characterPrefab, selectedTileView.transform.position + Vector3.up * 1.177f, quaternion.identity);
            newCharacterGO.transform.SetParent(soldiersParent, true);
            if (!newCharacterGO.TryGetComponent(out BaseCharacter baseCharacter))
                return;
            
            selectedTileView.SetSelectedUnit(baseCharacter);
            BattleManager.Instance.AddUnit(baseCharacter);
        }
    }
}