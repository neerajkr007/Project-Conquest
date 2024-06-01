using System;
using Game.Core.Characters;
using Game.Core.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Core.Views
{
    public class LoadoutSelectionTileView: MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private Image tileImage;
        private BaseCharacter selectedUnit;
        
        public BaseCharacter SelectedUnit => selectedUnit;

        private void Start()
        {
            SetTileSelectedState(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            LoadoutManager.Instance.OnLoadoutSelectionTileTapped(this);
        }

        public void SetSelectedUnit(BaseCharacter baseCharacter)
        {
            if (selectedUnit)
            {
                BattleManager.Instance.RemoveUnit(selectedUnit);
                Destroy(selectedUnit.gameObject);
            }
            
            selectedUnit = baseCharacter;
        }

        public void SetTileSelectedState(bool ifSelected)
        {
            tileImage.color = ifSelected ? Color.green : Color.gray;
            tileImage.color = new Color(tileImage.color.r, tileImage.color.g, tileImage.color.b, 0.3f);
        }
    }
}