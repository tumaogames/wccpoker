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
        [SerializeField] int _ownerSeatIndex = 4;

        [Space]

        [SerializeField] Sprite[] _sampleAvatars;
        [SerializeField] int _fallbackTurnTimeSeconds = 20;

        [Space]

        readonly List<PlayerHUD_UI> _playersHUDList = new();

        PlayerHUD_UI _currentPlayerHUD;

        public event Action<ConcurrentDictionary<string, PlayerHUD_UI>> PlayerHUDListListenerEvent;

        readonly ConcurrentDictionary<string, PlayerHUD_UI> _inGamePlayersRecords = new();
        readonly ConcurrentDictionary<string, int> _displayStacks = new();
        readonly ConcurrentDictionary<string, int> _betThisRound = new();
        readonly ConcurrentDictionary<string, int> _pendingWinAmounts = new();
        readonly ConcurrentDictionary<string, int> _playerServerSeats = new();
        bool _isSpectatorMode;

        TableState _currentTableState;

        public enum TagType { Dealer, SmallBlind, BigBlind }

        string _last_dealerId = "?";
        string _last_sbId = "?";
        string _last_bbId = "?";
        static long _serverTimeOffsetMs;
        const long ServerOffsetSnapThresholdMs = 2000;
        int _ownerSeat = -1;

        #region EDITOR
        private void OnValidate()
        {
            if (_maxPlayers > _playersTablePositions.Length) _maxPlayers = _playersTablePositions.Length;
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
                Debug.Log($"<color=green>WINNER-ID: {playerID} </color>");
                if (!_inGamePlayersRecords.ContainsKey(playerID)) return;
                var p = _inGamePlayersRecords[playerID];

                p.SetEnableWinner(true);
                AudioManager.main.PlayRandomAudio("Winner", Vector2.zero);

                await Task.Delay(3000);

                p.SetEnableWinner(false);
            };

            TableBetController.OnPotChipsMovedToWinner += OnPotChipsMovedToWinner;
        }

        void OnActionBroadcast(ActionBroadcast m)
        {
            if (_inGamePlayersRecords.ContainsKey(m.PlayerId))
            {
                _inGamePlayersRecords[m.PlayerId].SetCancelTurnTime();
                _inGamePlayersRecords[m.PlayerId].SetEnableActionHolder(true);
                _inGamePlayersRecords[m.PlayerId].SetActionBroadcast($"{m.Action}");

                if (m.Action == PokerActionType.Check) AudioManager.main.PlayAudio("Actions", 0);
                _inGamePlayersRecords[m.PlayerId].SetFoldedState(m.Action == PokerActionType.Fold);
            }

            if (!_inGamePlayersRecords.ContainsKey(m.PlayerId)) return;

            if (m.Action == PokerActionType.Bet ||
                m.Action == PokerActionType.Call ||
                m.Action == PokerActionType.Raise ||
                m.Action == PokerActionType.AllIn)
            {
                var currentBet = _betThisRound.TryGetValue(m.PlayerId, out var b) ? b : 0;
                var delta = m.Amount > 0 ? (int)m.Amount : Math.Max(0, (int)m.CurrentBet - currentBet);
                if (delta > 0)
                {
                    var stack = _displayStacks.TryGetValue(m.PlayerId, out var s) ? s : 0;
                    stack = Math.Max(0, stack - delta);
                    _displayStacks[m.PlayerId] = stack;
                    _betThisRound[m.PlayerId] = currentBet + delta;
                    _inGamePlayersRecords[m.PlayerId].UpdateChipsAmount(stack);
                }
            }
        }

        void OnTableSnapshot(TableSnapshot m)
        {
            var dealerId = "?";
            var sbId = "?";
            var bbId = "?";
            var ownerId = PokerNetConnect.OwnerPlayerID;

            foreach (var p in m.Players)
            {
                _playerServerSeats[p.PlayerId] = p.Seat;
                if (!string.IsNullOrEmpty(ownerId) && p.PlayerId == ownerId)
                    _ownerSeat = p.Seat;
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

            foreach (var p in m.Players)
            {
                if (!_inGamePlayersRecords.ContainsKey(p.PlayerId))
                    SummonPlayerHUDUI(p.Seat, $"P-{p.PlayerId}-{p.Seat}", p.PlayerId, (int)p.Stack);
                _displayStacks[p.PlayerId] = (int)p.Stack;
                _betThisRound[p.PlayerId] = (int)p.BetThisRound;
            }

            RefreshPlayerPositions();
            PlayerHUDListListenerEvent?.Invoke(_inGamePlayersRecords);

            if (m.State == TableState.Reset)
            {
                foreach (var p in _inGamePlayersRecords)
                    p.Value.SetEnableActionHolder(false);
                foreach (var p in _inGamePlayersRecords)
                    p.Value.SetFoldedState(false);
            }
            if (m.State == TableState.Waiting)
            {
                foreach (var p in _inGamePlayersRecords)
                    p.Value.SetEnableActionHolder(false);
                foreach (var p in _inGamePlayersRecords)
                    p.Value.SetFoldedState(false);
            }
        }

        void OnTurnUpdate(TurnUpdate m)
        {
            var remainingMs = GetRemainingTurnMs(m.DeadlineUnixMs);

            _displayStacks[m.PlayerId] = (int)m.Stack;
            if (_inGamePlayersRecords.TryGetValue(m.PlayerId, out var hud))
            {
                hud.UpdateChipsAmount((int)m.Stack);
                hud.SetTurn();
            }
            else
            {
                Debug.LogWarning($"[PlayerHUD] TurnUpdate for unknown player {m.PlayerId}. Waiting for snapshot.");
            }
            _inGamePlayersRecords[m.PlayerId].SetTurn(remainingMs);

            if (m.PlayerId == PokerNetConnect.OwnerPlayerID && m.Seat != _ownerSeat)
            {
                _ownerSeat = m.Seat;
                _playerServerSeats[m.PlayerId] = m.Seat;
                RefreshPlayerPositions();
                PlayerHUDListListenerEvent?.Invoke(_inGamePlayersRecords);
            }

            print($"<color=green>SERVER TIME: {remainingMs} | DeadlineUnixMs: {m.DeadlineUnixMs}</color>");
        }

        void OnPong(Pong m)
        {
            var serverMs = NormalizeUnixMs(m.TimestampUnixMs);
            if (serverMs <= 0)
                return;

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var offset = (long)serverMs - nowMs;
            if (Math.Abs(offset) >= ServerOffsetSnapThresholdMs)
                _serverTimeOffsetMs = offset;
            else if (Math.Abs(_serverTimeOffsetMs) < ServerOffsetSnapThresholdMs)
                _serverTimeOffsetMs = 0;
        }

        long GetRemainingTurnMs(ulong deadlineUnixMs)
        {
            var deadlineMs = NormalizeUnixMs(deadlineUnixMs);
            if (deadlineMs <= 0)
                return 0;

            var localNowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var nowMs = localNowMs + _serverTimeOffsetMs;
            var remaining = (long)deadlineMs - nowMs;
            if (remaining > 0)
                return remaining;

            if (_fallbackTurnTimeSeconds > 0)
            {
                var turnMs = (long)_fallbackTurnTimeSeconds * 1000L;
                _serverTimeOffsetMs = (long)deadlineMs - turnMs - localNowMs;
                remaining = (long)deadlineMs - (localNowMs + _serverTimeOffsetMs);
                return Math.Max(0, remaining);
            }

            return 0;
        }

        static ulong NormalizeUnixMs(ulong unixValue)
        {
            if (unixValue == 0)
                return 0;
            return unixValue < 1000000000000UL ? unixValue * 1000UL : unixValue;
        }

        void OnStackUpdate(StackUpdate m)
        {
            _displayStacks[m.PlayerId] = (int)m.Stack;
            if (_inGamePlayersRecords.TryGetValue(m.PlayerId, out var hud))
            {
                hud.UpdateChipsAmount((int)m.Stack);
            }
            else
            {
                Debug.LogWarning($"[PlayerHUD] StackUpdate for unknown player {m.PlayerId}. Waiting for snapshot.");
            }
        }

        void OnHandResult(HandResult m)
        {
            foreach (var winner in m.Winners)
            {
                if (!_inGamePlayersRecords.ContainsKey(winner.PlayerId)) continue;
                _pendingWinAmounts.AddOrUpdate(winner.PlayerId, (int)winner.Amount, (_, v) => v + (int)winner.Amount);
                _betThisRound[winner.PlayerId] = 0;
            }
        }

        void OnPotChipsMovedToWinner(string playerId)
        {
            if (!_pendingWinAmounts.TryRemove(playerId, out var winAmount)) return;
            if (!_inGamePlayersRecords.ContainsKey(playerId)) return;

            var current = _displayStacks.TryGetValue(playerId, out var s) ? s : 0;
            var updated = current + winAmount;
            _displayStacks[playerId] = updated;
            _inGamePlayersRecords[playerId].UpdateChipsAmount(updated);
        }

        void OnMessage(MsgType type, IMessage msg)
        {
            switch (type)
            {
                case MsgType.ActionBroadcast: OnActionBroadcast((ActionBroadcast)msg); break;
                case MsgType.TableSnapshot: OnTableSnapshot((TableSnapshot)msg); break;
                case MsgType.TurnUpdate: OnTurnUpdate((TurnUpdate)msg); break;
                case MsgType.Pong: OnPong((Pong)msg); break;
                case MsgType.SpectateResponse: OnSpectateResponse((SpectateResponse)msg); break;
                case MsgType.JoinTableResponse: OnJoinTableResponse((JoinTableResponse)msg); break;
                case MsgType.LeaveTableResponse: OnLeaveTableResponse((LeaveTableResponse)msg); break;
                case MsgType.StackUpdate: OnStackUpdate((StackUpdate)msg); break;
                case MsgType.HandResult: OnHandResult((HandResult)msg); break;
            }
        }

        void OnSpectateResponse(SpectateResponse m)
        {
            if (!m.Success)
                return;

            _isSpectatorMode = true;
            UpdateOwnerSpectatorState();
        }

        void OnJoinTableResponse(JoinTableResponse m)
        {
            if (!m.Success)
                return;

            _isSpectatorMode = false;
            UpdateOwnerSpectatorState();
        }

        void OnLeaveTableResponse(LeaveTableResponse m)
        {
            if (!m.Success)
                return;

            _isSpectatorMode = true;
            UpdateOwnerSpectatorState();
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

            var mappedIndex = MapSeatToIndex(seat);
            p.transform.localPosition = _playersTablePositions[mappedIndex].localPosition;

            var isOwner = playerID == PokerNetConnect.OwnerPlayerID;
            p.InititalizePlayerHUDUI(playerID, playerName, isOwner, 1, _sampleAvatars[UnityEngine.Random.Range(1, _sampleAvatars.Length)], stackAmount);
            p.SetSeatIndex(mappedIndex);

            if (playerID == PokerNetConnect.OwnerPlayerID)
            {
                _currentPlayerHUD = p;
                UpdateOwnerSpectatorState();
            }
        }

        void UpdateOwnerSpectatorState()
        {
            var ownerId = PokerNetConnect.OwnerPlayerID;
            if (string.IsNullOrEmpty(ownerId))
                return;

            if (_inGamePlayersRecords.TryGetValue(ownerId, out var hud))
                hud.SetSpectatorState(_isSpectatorMode);
        }

        void RefreshPlayerPositions()
        {
            foreach (var kvp in _inGamePlayersRecords)
            {
                var playerId = kvp.Key;
                var hud = kvp.Value;
                if (!_playerServerSeats.TryGetValue(playerId, out var seat))
                    continue;
                var mappedIndex = MapSeatToIndex(seat);
                hud.transform.localPosition = _playersTablePositions[mappedIndex].localPosition;
                hud.SetSeatIndex(mappedIndex);
            }
        }

        int MapSeatToIndex(int seat)
        {
            var total = _playersTablePositions.Length;
            if (total == 0)
                return 0;

            var ownerIndex = Mathf.Clamp(_ownerSeatIndex, 0, total - 1);
            if (_ownerSeat <= 0)
                return Mathf.Clamp(seat - 1, 0, total - 1);

            var offset = ownerIndex - (_ownerSeat - 1);
            var mapped = ((seat - 1 + offset) % total + total) % total;
            return mapped;
        }
    }
}
