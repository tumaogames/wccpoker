////////////////////
//       RECK       //
////////////////////


using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class TipController : MonoBehaviour
    {
        [SerializeField] TMP_InputField _tipIF;
        [SerializeField] Button _confirmButton;
     

        private void Start()
        {
            _confirmButton.onClick.AddListener(OnClickSendTipButton);
            _tipIF.onValueChanged.AddListener(OnChangeListener);
        }

        void OnClickSendTipButton()
        {

        }

        void OnChangeListener(string change)
        {
            _confirmButton.interactable = change != string.Empty;
        }
    }
}
