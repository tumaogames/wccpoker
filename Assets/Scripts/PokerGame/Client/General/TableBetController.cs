////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using DG.Tweening;
using Google.Protobuf;
using NaughtyAttributes;
using System;
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
        [SerializeField] BetValueUIText _betValueUITextPrefab;
        [SerializeField] GameObject _betValuePlusEffectPrefab;
        [SerializeField] Transform _betGroupContainer;
        [SerializeField] Transform[] _playersBetHolderPositions;
      

        [Header("[POT-GROUP]")]
        [SerializeField] GameObject _bigPotPrefab;
        [SerializeField] TMP_Text _potValueText;
        [SerializeField] GameObject _potHolder;

        readonly ConcurrentDictionary<string, BetValueUIText> _playerBetDictionary = new();
        readonly ConcurrentDictionary<string, (PlayerHUD_UI, int)> _playerHUDRecords = new();

        int _currentTotalPotValue = 0;

        TableState _currentTableState;

        public static event Action<string> OnPotChipsMovedToWinner;

        private void Awake() => PokerNetConnect.OnMessageEvent += OnMessage;

        private void Start()
        {
            PlayerHUDController.main.PlayerHUDListListenerEvent += LoadPlayerHUDList;
            DeckCardsController.OnWinnerEvent += MoveAllPotChipsToWinner;
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
            if(!_potHolder.activeInHierarchy) _potHolder.SetActive(true);
            foreach (var bet in _playerBetDictionary)
            {
                bet.Value.SetEnableValueHolder(false);
                bet.Value.transform.DOMove(_potHolder.transform.position, 0.15f)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    AudioManager.main.PlayAudio("Chips_Pot", 0);
                    _potValueText.text = FormatChips(_currentTotalPotValue);
                    Destroy(bet.Value.gameObject);
                });
            }
            _playerBetDictionary.Clear();
        }

        void MoveAllPotChipsToWinner(string playerID)
        {
            if(!_playerHUDRecords.ContainsKey(playerID)) return;
            InstantiateBet(_bigPotPrefab, 
                _potHolder.transform.position,
                _playerHUDRecords[playerID].Item1.transform.position,
                0.8f,
                Instance =>
            {
                _potHolder.SetActive(false);
                _potValueText.text = string.Empty;

                AudioManager.main.PlayAudio("Chips_Pot", 0);
                Destroy(Instance);
                OnPotChipsMovedToWinner?.Invoke(playerID);

            });
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
            if (m.Action == PokerActionType.Bet || m.Action == PokerActionType.AllIn || m.Action == PokerActionType.Call)
                SetBetPlayer((int)m.CurrentBet, m.PlayerId);
        }

        async void OnTableSnapshot(TableSnapshot m)
        {
            if (_currentTableState != m.State)
            {
                await Task.Delay(1000);

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
