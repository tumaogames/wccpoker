////////////////////
//       RECK       //
////////////////////


using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        [SerializeField] TMP_Text _actionText;

        [Header("[TURN]")]
        [SerializeField] GameObject _turnGroupGO;
        [SerializeField] Image _turnLoadingImgFill;
        [SerializeField] TMP_Text _countdownTimeText;
        [SerializeField] Image _turnWarningImage;

        [Header("[Effects]")]
        [SerializeField] Color _ownPlayerNameTextColor;
        [SerializeField] GameObject _turnHightlightGO;

        [Header("[EVENTS]")]
        [SerializeField] UnityEvent _isMineEvent;
        [SerializeField] UnityEvent _isNotMineEvent;
        [SerializeField] UnityEvent<bool> _isOwnerBoolEvent;
        [SerializeField] UnityEvent<bool> _onTurnWarningBoolEvent;
        [SerializeField] UnityEvent _onTurnCountdownEndEvent;

        bool _isOwner;
        string _playerID;
        readonly static WaitForSeconds _waitForSeconds1 = new(1f);

        public bool IsOwner => _isOwner;

        Coroutine _timerCoroutine;


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
            _isOwner = isMine;
            _levelText.text = $"{level}";
            _playerProfileImage.sprite = profile;
            _amountText.text = $"${amount}";
            CheckOwner(isMine);
        }

        public void UpdateChipsAmount(int amount)
        {
            _amountText.text = $"${amount}";
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
        public void SetTurn([Optional] Action callback)
        {
            _turnHightlightGO.SetActive(true);
            _timerCoroutine = StartCoroutine(TurnTime(() =>
            {
                _turnHightlightGO.SetActive(false);
                ChangeTheWarningImageAlpha(0f);
                _onTurnCountdownEndEvent?.Invoke();
                callback?.Invoke();
            }));
        }

        public void SetCancelTurnTime()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _turnLoadingImgFill.fillAmount = 0f;
                _turnHightlightGO.SetActive(false);
                _turnGroupGO.SetActive(false);
                _onTurnWarningBoolEvent?.Invoke(false);
                _timerCoroutine = null;
            }
        }

        /// <summary>
        /// On this function ay para mag change lang ng alpha value para sa _turnWarningImage
        /// </summary>
        /// <param name="alphaValue"></param>
        void ChangeTheWarningImageAlpha(float alphaValue)
        {
            var c = _turnWarningImage.color;
            c.a = alphaValue;
            _turnWarningImage.color = c;
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

        IEnumerator TurnTime(UnityAction callback)
        {
            _turnGroupGO.SetActive(true);

            float duration = 10f;
            float timeLeft = duration;
            var triggered30 = false;
            var triggered50 = false;

            _turnLoadingImgFill.color = Color.green;

            while (timeLeft > 0f)
            {
                timeLeft -= Time.deltaTime;

                float percent = timeLeft / duration;

                _turnLoadingImgFill.fillAmount = percent;
                _countdownTimeText.text = Mathf.Ceil(timeLeft).ToString();

               
                if (!triggered50 && percent <= 0.5f)
                {
                    triggered50 = true;
                    _turnLoadingImgFill.color = Color.yellow;
                }
                else if (!triggered30 && percent <= 0.3f)
                {
                    triggered30 = true;
                    _turnLoadingImgFill.color = Color.red;
                    _onTurnWarningBoolEvent?.Invoke(true);
                }

                yield return null;
            }

            _turnLoadingImgFill.fillAmount = 0f;
            _countdownTimeText.text = "0";

            _turnGroupGO.SetActive(false);
            _onTurnWarningBoolEvent?.Invoke(false);
            callback();
        }

        public void SetActionBroadcast(string message) => _actionText.text = message;

        public void SetEnableActionHolder(bool enable) => _actionText.transform.parent.gameObject.SetActive(enable);
    }
}
