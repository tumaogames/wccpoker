////////////////////
//       RECK       //
////////////////////


using Com.poker.Core;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
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

        [SerializeField] BaseAnimation _userButtonActions;

        readonly List<PlayerHUD_UI> _playersHUDList = new();

        PlayerHUD_UI _currentPlayerHUD;

        public event Action<List<PlayerHUD_UI>> PlayerHUDListListenerEvent;

        int _playerIndex = 3;

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
            for (int i = 0; i < _maxPlayers; i++)
            {
                SummonPlayerHUDUI(i);
            }

            await Task.Delay(1000);

            PlayerHUDListListenerEvent?.Invoke(_playersHUDList);

            TestRoundTurn();
        }

        void OnMessage(MsgType type, IMessage msg)
        {
            if (type == MsgType.TableSnapshot)
            {
                Debug.Log($"<color=green> TableSnapshot </color>");
                // Full table state (seats, stacks, pot, board).
                var m = (TableSnapshot)msg;
                // Data you can use:
                // - m.State (waiting/preflop/flop/turn/river/showdown/reset)
                // - m.CommunityCards (board cards if already revealed)
                // - m.PotTotal, m.CurrentBet, m.MinRaise
                // - m.CurrentTurnSeat
                Debug.Log($"[Snapshot] table={m.TableId} state={m.State} pot={m.PotTotal} currentBet={m.CurrentBet}");
                foreach (var p in m.Players)
                {
                    Debug.Log($"<color=blue>  seat={p.Seat} player={p.PlayerId} stack={p.Stack} bet={p.BetThisRound} status={p.Status} </color>");
                }
                // Community cards (if any are already revealed)
                // You can read them like:
                // - m.CommunityCards[0] -> first board card
                // - m.CommunityCards[1] -> second board card
                // If no board yet (pre-flop), count = 0.
                if (m.CommunityCards != null && m.CommunityCards.Count > 0)
                    Debug.Log($"  board={PokerNetConnect.FormatCards(m.CommunityCards)}");
            }
        }

        /// <summary>
        /// This function ay para mag instantiate ng player UIs
        /// </summary>
        /// <param name="i"></param>
        void SummonPlayerHUDUI(int i)
        {
            var p = Instantiate(_playerHUDPrefab, _playersContainer);
            _playersHUDList.Add(p);
            p.transform.localPosition = i == _playerIndex ? _playersTablePositions[_playerIndex].localPosition : _playersTablePositions[i].localPosition;

            p.InititalizePlayerHUDUI("ID3423", i == _playerIndex ? "You" : $"Name-{UnityEngine.Random.Range(111,9999)}", i == _playerIndex, 1, _sampleAvatars[UnityEngine.Random.Range(1, _sampleAvatars.Length)], UnityEngine.Random.Range(100, 999));
            
            if(i == 0 && _currentPlayerHUD == null) _currentPlayerHUD = p;
        }

        async void TestRoundTurn()
        {
            foreach (var p in _playersHUDList)
            {
                var tcs = new TaskCompletionSource<bool>();

                if (p == _currentPlayerHUD) _userButtonActions.PlayAnimation("PlayActionButtonGoUp");

                p.SetTurn(() =>
                {
                    tcs.SetResult(true);
                    if (p == _currentPlayerHUD) _userButtonActions.PlayAnimation("PlayActionButtonGoDown");
                });

                await tcs.Task; 
            }
        }
    }
}
