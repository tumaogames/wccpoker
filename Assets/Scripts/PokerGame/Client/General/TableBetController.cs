////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using DG.Tweening;
using Google.Protobuf;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using WCC.Core.Audio;


namespace WCC.Poker.Client
{
    public class TableBetController : MonoBehaviour
    {
        public static TableBetController Instance { get; private set; }
        [SerializeField] BetValueUIText _betValueUITextPrefab;
        [SerializeField] GameObject _betValuePlusEffectPrefab;
        [SerializeField] Transform _betGroupContainer;
        [SerializeField] Transform[] _playersBetHolderPositions;
        [SerializeField] float _betMoveToPotDuration = 0.35f;

        [Header("[POT-GROUP]")]
        [SerializeField] GameObject _bigPotPrefab;
        [SerializeField] TMP_Text _potValueText;
        [SerializeField] GameObject _potHolder;

        readonly ConcurrentDictionary<string, BetValueUIText> _playerBetDictionary = new();
        readonly ConcurrentDictionary<string, (PlayerHUD_UI, int)> _playerHUDRecords = new();

        int _currentTotalPotValue = 0;
        int _pendingMoveToPot = 0;
        bool _isMovingToPot = false;

        TableState _currentTableState;

        public static event Action<string> OnPotChipsMovedToWinner;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            PokerNetConnect.OnMessageEvent += OnMessage;
        }

        private void Start()
        {
            _potHolder.SetActive(false);
            PlayerHUDController.main.PlayerHUDListListenerEvent += LoadPlayerHUDList;
            DeckCardsController.OnWinnerEvent += MoveAllPotChipsToWinners;
        }

        #region DEBUG
        [Space(20)]
        public int Debug_playerBetDictionary;
        public int Debug_playerHUDRecords;

        private void Update()
        {
            Debug_playerBetDictionary = _playerBetDictionary.Count;
            Debug_playerHUDRecords = _playerHUDRecords.Count;
        }
        #endregion DEBUG

        void LoadPlayerHUDList(ConcurrentDictionary<string, PlayerHUD_UI> data)
        {
            foreach (var i in data)
            {
                if (_playerHUDRecords.TryGetValue(i.Key, out var existing))
                {
                    if (existing.Item2 != i.Value.SeatIndex)
                        _playerHUDRecords[i.Key] = (i.Value, i.Value.SeatIndex);
                    continue;
                }

                _playerHUDRecords.TryAdd(i.Key, (i.Value, i.Value.SeatIndex));
            }
        }

        [Button]
        public void SetDebug_BetRandomPlayers()
        {
            //SetBetPlayer(UnityEngine.Random.Range(50, 2000), UnityEngine.Random.Range(0, _playersBetHolderPositions.Length));
        }

        /// <summary>
        /// This function ay para mag set ng bet sa player using PLAYER ID
        /// </summary>
        /// <param name="betValue"></param>
        /// <param name="playerID"></param>
        public void SetBetPlayer(int betValue, string playerID) => StartBetting(betValue, playerID);

        void StartBetting(int betValue, string playerID)
        {
            if(_playerHUDRecords.Count == 0) return;
            if (_playerBetDictionary.ContainsKey(playerID))
            {
                InstantiateBet(_betValuePlusEffectPrefab,
                    _playerHUDRecords[playerID].Item1.transform.position, 
                    _playersBetHolderPositions[_playerHUDRecords[playerID].Item2].position,
                    0.22f,
                    Instance =>
                {
                    Instance.transform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack).OnComplete(() =>
                    {
                        EditTableBet(betValue, _playerBetDictionary[playerID]);
                        Destroy(Instance);
                    });
                });

                return;
            }

            //if(!_playerHUDRecords.ContainsKey(playerID)) return;
            InstantiateBet(_betValueUITextPrefab.gameObject,
                _playerHUDRecords[playerID].Item1.transform.position,
                _playersBetHolderPositions[_playerHUDRecords[playerID].Item2].position,
                0.22f,
                Instance =>
            {
                var bvuiT = Instance.GetComponent<BetValueUIText>();
                _playerBetDictionary.TryAdd(playerID, bvuiT);
                bvuiT.SetBetValue(betValue);
            });
         
        }

        void InstantiateBet(GameObject prefab, Vector2 startPosition, Vector2 destination, float moveDuration, [Optional] UnityAction<GameObject> isReachedCallback)
        {
            var betHolder = Instantiate(prefab, _betGroupContainer);
            AudioManager.main.PlayRandomAudio("Chips_Bet", Vector2.zero);
            betHolder.transform.position = startPosition;
            if (GameServerClient.Instance.IsCatchingUp)
            {
                betHolder.transform.position = destination;
                betHolder.transform.localScale = Vector2.one;
                betHolder.transform.localRotation = Quaternion.Euler(0, 0, 0);
                isReachedCallback?.Invoke(betHolder);
                return;
            }

            betHolder.transform.DOMove(destination, moveDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                betHolder.transform.localScale = Vector2.one;
                betHolder.transform.localRotation = Quaternion.Euler(0, 0, 0);
                isReachedCallback?.Invoke(betHolder);
            });
        }

        void EditTableBet(int newBetValue, BetValueUIText betValueUIText) => betValueUIText.SetBetValue(newBetValue);

        [Button]
        public void MoveAllBetsToPot()
        {
            if (_playerBetDictionary.Count == 0) return;
            StartCoroutine(MoveAllBetsToPotRoutine());
        }

        public IEnumerator MoveAllBetsToPotRoutine()
        {
            if (_playerBetDictionary.Count == 0)
                yield break;

            _isMovingToPot = true;
            if(!_potHolder.activeInHierarchy) _potHolder.SetActive(true);
            _pendingMoveToPot = _playerBetDictionary.Count;
            var movingBets = new List<GameObject>(_pendingMoveToPot);

            foreach (var bet in _playerBetDictionary)
            {
                bet.Value.SetEnableValueHolder(false);
                movingBets.Add(bet.Value.gameObject);
                bet.Value.transform.DOMove(_potHolder.transform.position, _betMoveToPotDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    AudioManager.main.PlayAudio("Chips_Pot", 0);
                    _potValueText.text = FormatChips(_currentTotalPotValue);
                    Destroy(bet.Value.gameObject);
                    _pendingMoveToPot--;
                });
            }
            _playerBetDictionary.Clear();

            yield return new WaitUntil(() => _pendingMoveToPot <= 0);
            yield return new WaitUntil(() =>
            {
                for (int i = 0; i < movingBets.Count; i++)
                {
                    if (movingBets[i] != null)
                        return false;
                }
                return true;
            });
            _isMovingToPot = false;
        }

        public IEnumerator WaitForBetsToPot()
        {
            if (_isMovingToPot)
            {
                yield return new WaitUntil(() => !_isMovingToPot);
                yield break;
            }

            if (_playerBetDictionary.Count > 0)
                yield return MoveAllBetsToPotRoutine();
        }

        void MoveAllPotChipsToWinners(IReadOnlyList<string> playerIds)
        {
            if (playerIds == null || playerIds.Count == 0)
                return;

            if (!_potHolder.activeInHierarchy)
                _potHolder.SetActive(true);

            int pending = 0;
            foreach (var playerID in playerIds)
            {
                if (string.IsNullOrEmpty(playerID))
                    continue;
                if (!_playerHUDRecords.ContainsKey(playerID))
                    continue;

                pending++;
                InstantiateBet(_bigPotPrefab,
                    _potHolder.transform.position,
                    _playerHUDRecords[playerID].Item1.transform.position,
                    0.8f,
                    Instance =>
                {
                    AudioManager.main.PlayAudio("Chips_Pot", 0);
                    Destroy(Instance);
                    OnPotChipsMovedToWinner?.Invoke(playerID);

                    pending--;
                    if (pending <= 0)
                    {
                        _potHolder.SetActive(false);
                        _potValueText.text = string.Empty;
                    }
                });
            }

            if (pending == 0)
            {
                _potHolder.SetActive(false);
                _potValueText.text = string.Empty;
            }
        }

        string FormatChips(int value)
        {
            if (value >= 1_000_000)
                return (value / 1_000f).ToString("0.#") + "M";
            if (value >= 1_000)
                return (value / 1_000f).ToString("0.#") + "K";

            return value.ToString();
        }

        void OnMessage(MsgType type, IMessage msg)
        {
            switch (type)
            {
                case MsgType.ActionBroadcast: OnActionBroadcast((ActionBroadcast)msg); break;
                case MsgType.TableSnapshot: OnTableSnapshot((TableSnapshot)msg); break;
                case MsgType.PotUpdate: OnPotUpdate((PotUpdate)msg); break;
                case MsgType.HandResult: OnHandResult((HandResult)msg); break;
            }
        }

        void OnActionBroadcast(ActionBroadcast m)
        {
            if (m.Action == PokerActionType.Bet ||
                m.Action == PokerActionType.AllIn ||
                m.Action == PokerActionType.Call ||
                m.Action == PokerActionType.Raise)
            {
                SetBetPlayer((int)m.CurrentBet, m.PlayerId);
            }
        }

        async void OnTableSnapshot(TableSnapshot m)
        {
            if (_currentTableState != m.State)
            {
                //await Task.Delay(300);
                MoveAllBetsToPot();

                _currentTableState = m.State;
            }
        }

        void OnPotUpdate(PotUpdate m)
        {
            _currentTotalPotValue = (int)m.PotTotal;
        }

        void OnHandResult(HandResult m)
        {

        }

    }
}
