////////////////////
//       RECK       //
////////////////////


using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class PlayerHUD_UI : MonoBehaviour
    {
        [Header("[REFERENCES]")]
        [SerializeField] TMP_Text _playerNameText;
        [SerializeField] Image _playerProfileImage;
        [SerializeField] TMP_Text _amountText;
        [SerializeField] TMP_Text _levelText;

        [Space]

        [SerializeField] GameObject _turnHightlightGO;

        [Header("[OP]")]
        [SerializeField] Color _ownPlayerNameTextColor;

        [Header("[EVENTS]")]
        [SerializeField] UnityEvent _isMineEvent;
        [SerializeField] UnityEvent _isNotMineEvent;
        [SerializeField] UnityEvent<bool> _isOwnerBoolEvent;

        string _playerID;

        //
        public void InititalizePlayerHUDUI(string id, string playerName, bool isMine, int level, Sprite profile, int amount)
        {
            _playerID = id;
            _playerNameText.text = playerName;
            _levelText.text = $"{level}";
            _playerProfileImage.sprite = profile;
            _amountText.text = $"${amount}";
            CheckOwner(isMine);
        }

        void CheckOwner(bool isMine)
        {
            if (isMine)
            {
                _isMineEvent?.Invoke();
                _playerNameText.color = _ownPlayerNameTextColor;
            }
            else _isNotMineEvent?.Invoke();
            _isOwnerBoolEvent?.Invoke(isMine);
        }

        public void SetTurn()
        {
            _turnHightlightGO.SetActive(true);
        }

        public void SetGlowEffect()
        {

        }
    }
}
