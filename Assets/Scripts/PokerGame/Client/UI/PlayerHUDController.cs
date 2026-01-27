////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WCC.Core.Audio;
using WCC.Core.Exposed;

namespace WCC.Poker.Client
{
    public class PlayerHUDController : Exposing<PlayerHUDController>
    {
       
        [SerializeField] PlayerHUD_UI _playerHUDPrefab;
        [SerializeField] Transform[] _playersTablePositions;
        [SerializeField] Transform _playersContainer;

        [Space]

        [SerializeField] int _maxPlayers = 3;

        [Space]

        [SerializeField] Sprite[] _sampleAvatars;

        [Space]

        readonly List<PlayerHUD_UI> _playersHUDList = new();

        PlayerHUD_UI _currentPlayerHUD;

        public event Action<ConcurrentDictionary<string, PlayerHUD_UI>> PlayerHUDListListenerEvent;

        int _playerIndex = 4;

        readonly ConcurrentDictionary<string, PlayerHUD_UI> _inGamePlayersRecords = new();

        TableState _currentTableState;

        public enum TagType { Dealer, SmallBlind, BigBlind }

        string _last_dealerId = "?";
        string _last_sbId = "?";
        string _last_bbId = "?";

        #region EDITOR
        private void OnValidate()
        {
            if(_maxPlayers > _playersTablePositions.Length) _maxPlayers = _playersTablePositions.Length;
        }
        #endregion EDITOR

        protected override void Awake()
        {
            base.Awake();
            PokerNetConnect.OnMessageEvent += OnMessage;
        }

        private async void Start()
        {
            DeckCardsController.OnWinnerEvent += async playerID =>
            {
                if (!_inGamePlayersRecords.ContainsKey(playerID)) return;
                var p = _inGamePlayersRecords[playerID];

                p.SetEnableWinner(true);
                AudioManager.main.PlayRandomAudio("Winner", Vector2.zero);

                await Task.Delay(3000);

                p.SetEnableWinner(false);
            };
        }

        void OnActionBroadcast(ActionBroadcast m)
        {
            if (_inGamePlayersRecords.ContainsKey(m.PlayerId))
            {
                _inGamePlayersRecords[m.PlayerId].SetCancelTurnTime();
                _inGamePlayersRecords[m.PlayerId].SetEnableActionHolder(true);
                _inGamePlayersRecords[m.PlayerId].SetActionBroadcast($"{m.Action}");

                if (m.Action == PokerActionType.Check) AudioManager.main.PlayAudio("Actions", 0);
            }
        }

        void OnTableSnapshot(TableSnapshot m)
        {
            var dealerId = "?";
            var sbId = "?";
            var bbId = "?";

            foreach (var p in m.Players)
            {
                if (p.Seat == m.DealerSeat)
                {
                    if (_inGamePlayersRecords.ContainsKey(_last_dealerId))
                        _inGamePlayersRecords[_last_dealerId].SetTag(TagType.Dealer, false);
                    dealerId = p.PlayerId;
                }
                if (p.Seat == m.SmallBlindSeat)
                {
                    if (_inGamePlayersRecords.ContainsKey(_last_sbId))
                        _inGamePlayersRecords[_last_sbId].SetTag(TagType.Dealer, false);
                    sbId = p.PlayerId;
                }
                if (p.Seat == m.BigBlindSeat)
                {
                    if (_inGamePlayersRecords.ContainsKey(_last_bbId))
                        _inGamePlayersRecords[_last_bbId].SetTag(TagType.Dealer, false);
                    bbId = p.PlayerId;
                }
            }

            if (_inGamePlayersRecords.Count != 0)
            {
                if (_inGamePlayersRecords.ContainsKey(dealerId))
                {
                    _inGamePlayersRecords[dealerId].SetTag(TagType.Dealer, true);
                    _last_dealerId = dealerId;
                }
                if (_inGamePlayersRecords.ContainsKey(sbId))
                {
                    _inGamePlayersRecords[sbId].SetTag(TagType.SmallBlind, true);
                    _last_sbId = sbId;
                }
                if (_inGamePlayersRecords.ContainsKey(bbId))
                {
                    _inGamePlayersRecords[bbId].SetTag(TagType.BigBlind, true);
                    _last_bbId = bbId;
                }
            }


            if (_currentTableState != m.State)
            {
                foreach (var p in _inGamePlayersRecords)
                    p.Value.SetEnableActionHolder(false);

                _currentTableState = m.State;
            }

            if (_inGamePlayersRecords.Count == 0)
            {
                foreach (var p in m.Players)
                {
                    if (_inGamePlayersRecords.ContainsKey(p.PlayerId)) continue;
                    SummonPlayerHUDUI(p.Seat - 1, $"P-{p.PlayerId}-{p.Seat}", p.PlayerId, (int)p.Stack);
                }

                PlayerHUDListListenerEvent?.Invoke(_inGamePlayersRecords);
            }

            if (m.State == TableState.Reset)
            {
                foreach (var p in _inGamePlayersRecords)
                    p.Value.SetEnableActionHolder(false);
            }
        }

        void OnTurnUpdate(TurnUpdate m)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var remainingMs = m.DeadlineUnixMs > 0 ? Math.Max(0, (long)m.DeadlineUnixMs - nowMs) : 0;
            _inGamePlayersRecords[m.PlayerId].UpdateChipsAmount((int)m.Stack);
            _inGamePlayersRecords[m.PlayerId].SetTurn();
        }

        void OnStackUpdate(StackUpdate m)
        {
            _inGamePlayersRecords[m.PlayerId].UpdateChipsAmount((int)m.Stack);
        }

        void OnMessage(MsgType type, IMessage msg)
        {
            switch (type)
            {
                case MsgType.ActionBroadcast: OnActionBroadcast((ActionBroadcast)msg); break;
                case MsgType.TableSnapshot: OnTableSnapshot((TableSnapshot)msg); break;
                case MsgType.TurnUpdate: OnTurnUpdate((TurnUpdate)msg); break;
                case MsgType.StackUpdate: OnStackUpdate((StackUpdate)msg); break;
            }
        }

        /// <summary>
        /// This function ay para mag instantiate ng player UIs
        /// </summary>
        /// <param name="i"></param>
        void SummonPlayerHUDUI(int seat, string playerName, string playerID, int stackAmount)
        {
            var p = Instantiate(_playerHUDPrefab, _playersContainer);
            _playersHUDList.Add(p);
            _inGamePlayersRecords.TryAdd(playerID, p);

            p.transform.localPosition = seat == _playerIndex ? _playersTablePositions[_playerIndex].localPosition : _playersTablePositions[seat].localPosition;

            p.InititalizePlayerHUDUI(playerID, playerName, seat == _playerIndex, 1, _sampleAvatars[UnityEngine.Random.Range(1, _sampleAvatars.Length)], stackAmount);
            
            if(seat == _playerIndex && _currentPlayerHUD == null) _currentPlayerHUD = p;
        }
    }
}
