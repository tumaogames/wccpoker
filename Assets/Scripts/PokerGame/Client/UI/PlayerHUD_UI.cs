////////////////////
//       RECK       //
////////////////////


using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class PlayerHUD_UI : MonoBehaviour
    {

        [SerializeField] TMP_Text _playerNameText;
        [SerializeField] Image _playerProfileImage;
        [SerializeField] TMP_Text _amountText;

        string _playerID;

        //
        public void InititalizePlayerHUDUI(string id, string playerName, Sprite profile, int amount)
        {
            _playerID = id;
            _playerNameText.text = playerName;
            _playerProfileImage.sprite = profile;
            _amountText.text = $"${amount}";
        }
    }
}
