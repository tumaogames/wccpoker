////////////////////
//       RECK       //
////////////////////


using System;
using System.Collections;
using System.Runtime.InteropServices;
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
        [SerializeField] GameObject _actionHolder;

        [Header("[TURN]")]
        [SerializeField] GameObject _turnGroupGO;
        [SerializeField] Image _turnLoadingImgFill;
        [SerializeField] TMP_Text _countdownTimeText;
        [SerializeField] Image _turnWarningImage;
        [SerializeField] GameObject _winnerGO;

        [Header("[Effects]")]
        [SerializeField] Color _ownPlayerNameTextColor;
        [SerializeField] GameObject _turnHightlightGO;

        [SerializeField] GameObject _tagBG;
        [SerializeField] GameObject[] _tagIcons;

        [Header("[EVENTS]")]
        [SerializeField] UnityEvent _isMineEvent;
        [SerializeField] UnityEvent _isNotMineEvent;
        [SerializeField] UnityEvent<bool> _isOwnerBoolEvent;
        [SerializeField] UnityEvent<bool> _onTurnWarningBoolEvent;
        [SerializeField] UnityEvent _onTurnCountdownEndEvent;
        [SerializeField] UnityEvent<bool> _onFoldingActionBoolEvent;
        [SerializeField] UnityEvent _isFoldActionEvent;
        [SerializeField] UnityEvent _isUnfoldActionEvent;
        [SerializeField] UnityEvent<bool> _onSpectatorBoolEvent;
        [SerializeField] UnityEvent _isSpectatorEvent;
        [SerializeField] UnityEvent _isUnspectatorEvent;

        bool _isOwner;
        int _seatIndex = -1;
        readonly static WaitForSeconds _waitForSeconds1 = new(1f);

        public bool IsOwner => _isOwner;
        public int SeatIndex => _seatIndex;

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
            _playerNameText.text = playerName;
            _isOwner = isMine;
            _levelText.text = $"{level}";
            _playerProfileImage.sprite = profile;
            _amountText.text = $"${amount}";
            CheckOwner(isMine);
        }

        public void SetSeatIndex(int seatIndex) => _seatIndex = seatIndex;

        public void UpdateChipsAmount(int amount) => _amountText.text = $"${amount}";

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
            SetTurn(0, callback);
        }

        public void SetTurn(long remainingMs, [Optional] Action callback)
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            _turnHightlightGO.SetActive(true);
            _timerCoroutine = StartCoroutine(TurnTime(remainingMs, () =>
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
        public void SetEnableWinner(bool e) => _winnerGO.SetActive(e);

        #region DEBUG-ONLY
        [NaughtyAttributes.Button] public void Debug_SetOwner() => CheckOwner(true);
        [NaughtyAttributes.Button] public void Debug_SetNotOwner() => CheckOwner(false);
        #endregion DEBUG-ONLY

        IEnumerator TurnTime(long remainingMs, UnityAction callback)
        {
            _turnGroupGO.SetActive(true);

            if (remainingMs <= 0)
            {
                _turnLoadingImgFill.fillAmount = 0f;
                _countdownTimeText.text = "0";
                _turnGroupGO.SetActive(false);
                _onTurnWarningBoolEvent?.Invoke(false);
                callback();
                yield break;
            }

            float duration = Mathf.Max(0.1f, remainingMs / 1000f) + 10f;
            float timeLeft = duration;
            var triggered30 = false;
            var triggered50 = false;

            _turnLoadingImgFill.color = Color.green;

            while (timeLeft > 0f)
            {
                timeLeft -= Time.unscaledDeltaTime;

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

        void SetEnableTag(int i, bool e)
        {
            foreach(var j in _tagIcons) j.SetActive(false);
            _tagIcons[i].SetActive(e);

            _tagBG.SetActive(e);
        }

        public void SetTag(PlayerHUDController.TagType tagType, bool e) => SetEnableTag((int)tagType, e);

        public void SetActionBroadcast(string message)
        {
            _actionText.text = message;
            _actionHolder.SetActive(_actionText.text != string.Empty);
        }

        public void SetEnableActionHolder(bool enable) => _actionHolder.SetActive(enable);

      
        public void SetFoldedState(bool isFolded)
        {
            _onFoldingActionBoolEvent?.Invoke(isFolded);
            UnityEvent actEvent = isFolded ? _isFoldActionEvent : _isUnfoldActionEvent;
            actEvent?.Invoke();
            if (isFolded)
                ChangeTheWarningImageAlpha(0f);

            print($"<color=blue>SetFoldedState at PlayerHUD_UI.cs</color>");
        }

        // Spectator mode hook: use this to drive spectator visuals (badge, dim, disable buttons).
        public void SetSpectatorState(bool isSpectator)
        {
            _onSpectatorBoolEvent?.Invoke(isSpectator);
            UnityEvent actEvent = isSpectator ? _isSpectatorEvent : _isUnspectatorEvent;
            actEvent?.Invoke();

            print($"<color=blue>SetSpectatorState at PlayerHUD_UI.cs</color>");
        }
    }
}
