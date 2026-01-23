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

        [Header("[Effects]")]
        [SerializeField] Color _ownPlayerNameTextColor;
        [SerializeField] GameObject _turnHightlightGO;

        [Header("[EVENTS]")]
        [SerializeField] UnityEvent _isMineEvent;
        [SerializeField] UnityEvent _isNotMineEvent;
        [SerializeField] UnityEvent<bool> _isOwnerBoolEvent;

        string _playerID;

        /// <summary>
        /// This function ay para sa initialize ng player
        /// </summary>
        /// <param name="id"></param>
        /// <param name="playerName"></param>
        /// <param name="isMine"></param>
        /// <param name="level"></param>
        /// <param name="profile"></param>
        /// <param name="amount"></param>
        public void InititalizePlayerHUDUI(string id, string playerName, bool isMine, int level, Sprite profile, int amount)
        {
            _playerID = id;
            _playerNameText.text = playerName;
            _levelText.text = $"{level}";
            _playerProfileImage.sprite = profile;
            _amountText.text = $"${amount}";
            CheckOwner(isMine);
        }

        /// <summary>
        /// This function ay para mag check kung sino owner HUD
        /// </summary>
        /// <param name="isMine"></param>
        void CheckOwner(bool isMine)
        {
            if (isMine)
            {
                _isMineEvent?.Invoke();
                _playerNameText.color = _ownPlayerNameTextColor;
            }
            else _isNotMineEvent?.Invoke();

            _isOwnerBoolEvent?.Invoke(isMine);

            transform.localScale = isMine ? new(0.8f, 0.8f, 0.8f) : new(0.6f, 0.6f, 0.6f);
        }

        /// <summary>
        /// This function ay para sa set kung sa kanya na yung TURN
        /// </summary>
        [NaughtyAttributes.Button]
        public void SetTurn()
        {
            _turnHightlightGO.SetActive(true);
        }

        /// <summary>
        /// This function ay para sa effects lamang
        /// </summary>
        [NaughtyAttributes.Button]
        public void SetEffect()
        {

        }

        #region DEBUG-ONLY
        [NaughtyAttributes.Button] public void Debug_SetOwner() => CheckOwner(true);
        [NaughtyAttributes.Button] public void Debug_SetNotOwner() => CheckOwner(false);
        #endregion DEBUG-ONLY
    }
}
