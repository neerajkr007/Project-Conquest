using Game.Core;
using Game.Core.Views;
using UnityEngine;
using Utils;

namespace Managers
{
    public class BattleUIManager : MonoSingleton<BattleUIManager>
    {
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private SurrenderPopupView surrenderPanel;

        public void Initialize()
        {
            surrenderPanel.Initialize(BattleManager.Instance.HandleSurrender);
            
            UpdateHuds();
        }

        public void UpdateHuds()
        {
            hudPanel.SetActive(BattleManager.Instance.BattleState == BattleState.PreparingPhase);
            surrenderPanel.gameObject.SetActive(BattleManager.Instance.BattleState == BattleState.Paused);
        }
    }
}