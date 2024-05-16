using System;
using Game.Core.Views;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Core.Managers
{
    public class LoadoutManager : MonoBehaviour
    {
        public static LoadoutManager Instance;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        [SerializeField] private GameObject SoldierPrefab;
        
        [SerializeField] private GameObject loadoutGO;
        
        private LoadoutSelectionTileView selectedTileView;

        public void OnLoadoutSelectionTileTapped(LoadoutSelectionTileView loadoutSelectionTileView)
        {
            if(selectedTileView == loadoutSelectionTileView)
                return;
            
            selectedTileView = loadoutSelectionTileView;
            
            if(loadoutGO.activeInHierarchy)
                return;
            
            loadoutGO.SetActive(true);
        }

        public void OnLoadoutOptionTapped(int index)
        {
            if(index == 0)
            {
                BattleManager.Instance.RemoveUnit(selectedTileView.SelectedUnit);
                selectedTileView.SetSelectedUnit(null);
                return;
            }
            
            GameObject soldierGO = Instantiate(SoldierPrefab, selectedTileView.transform.position + Vector3.up * 1.177f, quaternion.identity);
            BaseCharacter baseCharacter = soldierGO.GetComponent<BaseCharacter>();
            selectedTileView.SetSelectedUnit(baseCharacter);
            BattleManager.Instance.AddUnit(baseCharacter);
        }
    }
}