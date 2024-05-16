using Game.Core.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Core.Views
{
    public class LoadoutSelectionTileView: MonoBehaviour, IPointerDownHandler
    {
        private BaseCharacter selectedUnit;
        
        public BaseCharacter SelectedUnit => selectedUnit;
        
        public void OnPointerDown(PointerEventData eventData)
        {
            LoadoutManager.Instance.OnLoadoutSelectionTileTapped(this);
        }

        public void SetSelectedUnit(BaseCharacter baseCharacter)
        {
            if (selectedUnit)
                Destroy(selectedUnit.gameObject);
            
            selectedUnit = baseCharacter;
        }
    }
}