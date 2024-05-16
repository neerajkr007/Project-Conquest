using Game.Core.Managers;
using UnityEngine;

namespace Game.Core.Views
{
    public class LoadoutOptionView : MonoBehaviour
    {
        public void OptionSelected(int index)
        {
            LoadoutManager.Instance.OnLoadoutOptionTapped(index);
        }
    }
}