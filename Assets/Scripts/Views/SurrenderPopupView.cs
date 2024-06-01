using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.Views
{
    public class SurrenderPopupView : MonoBehaviour
    {
        [SerializeField] private Button acceptSurrenderBtn;
        [SerializeField] private Button noMercyBtn;

        private Action<bool> onClickedAction;

        public void Initialize(Action<bool> onClickedAction)
        {
            this.onClickedAction = onClickedAction;
        }

        private void OnEnable()
        {
            acceptSurrenderBtn.onClick.AddListener(OnAcceptSurrenderClicked);
            noMercyBtn.onClick.AddListener(OnNoMercyClicked);
        }

        private void OnDisable()
        {
            acceptSurrenderBtn.onClick.RemoveListener(OnAcceptSurrenderClicked);
            noMercyBtn.onClick.RemoveListener(OnNoMercyClicked);
        }

        private void OnAcceptSurrenderClicked()
        {
            onClickedAction?.Invoke(true);
            ClosePopup();
        }

        private void OnNoMercyClicked()
        {
            onClickedAction?.Invoke(false);
            ClosePopup();
        }

        private void ClosePopup()
        {
            gameObject.SetActive(false);
        }
    }
}