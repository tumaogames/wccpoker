////////////////////
//       RECK       //
////////////////////

using Com.poker.Core;
using DG.Tweening;
using Google.Protobuf;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WCC.Poker.Shared;


namespace WCC.Poker.Client
{
    public class DeckCardsController : MonoBehaviour
    {
        [Header("[PREFAB]")]
        [SerializeField] CardView _cardViewPrefab;

        [Header("[CONTAINERS]")]
        [SerializeField] Transform _deckTableGroup;
        [SerializeField] Transform _bankerPosition;
        [SerializeField] Transform _communityTableContainer;

        [Header("[PLAYER-TABLES]")]
        [SerializeField] Transform[] _playerTablePositions;
        [SerializeField] float _otherPlayerCardsSize = 0.8f;
        [SerializeField] float _playerCardsSize = 1.3f;

        readonly List<CardView> _cardViewList_onPlayers = new();
        readonly List<CardView> _cardViewList_onCommunity = new();

        readonly WaitForSeconds _drawDelay = new(0.11f);

        Vector2 _playerCardsSizeSets;
        Vector2 _otherCardsSizeSets;

        bool _isBankerDealsCardsForPlayers = false;
        bool _isBankerDealingCardsForCommunity = false;

        [SerializeField] List<PlayerPairCars> _debugCardsToDraw = new();
        [SerializeField] List<Card> _debugCardsToDrawCommmunity = new();

        readonly Dictionary<Com.poker.Core.Card, CardView> _communityCardsRecords = new();

        [SerializeField] TMP_Text _debugRoundNameText;

        readonly Dictionary<string, int> _playerSeatRecords = new();

        [Serializable]
        class PlayerPairCars
        {
            [SerializeField] internal Card[] _cards;
        }

        void Awake() => PokerNetConnect.OnMessageEvent += OnMessage;

        private void Start()
        {
            _playerCardsSizeSets = new Vector2(_playerCardsSize, _playerCardsSize);
            _otherCardsSizeSets = new Vector2(_otherPlayerCardsSize, _otherPlayerCardsSize);
        }

        [Button]
        public void SetBankerDealCards()
        {
            if (_isBankerDealsCardsForPlayers) return;
            //StartCoroutine(DealCardsForPlayers(_debugCardsToDraw));
        }

        [Button]
        public void SetBankerDealCardsForCommunity()
        {
            if(_isBankerDealingCardsForCommunity) return;
            //StartCoroutine(DealCardsForCommunity(_debugCardsToDrawCommmunity));
        }

        [Button]
        public void SetShowCards()
        {
            _cardViewList_onPlayers.ForEach(i => i.OpenCard());
        }

        [Button]
        public void SetCloseCards()
        {
            _cardViewList_onPlayers.ForEach(i => i.CloseCard());
        }

        [Button]
        public void SetClosePlayerCardsOnly()
        {
            _cardViewList_onPlayers[0].CloseCard();
            _cardViewList_onPlayers[1].CloseCard();
        }

        [Button]
        public void SetReturnCardsToBanker()
        {
            StartCoroutine(ReturnAllCardsToBanker());
        }

        


        /// <summary>
        /// On this coroutine ang banker ay nag bibigay ng mga braha sa each players
        /// </summary>
        /// <param name="infoList"></param>
        /// <returns></returns>
        //IEnumerator DealCardsForCommunity(List<Card> infoList)
        //{
        //    _isBankerDealingCardsForCommunity = true;

        //    for (int i = 0; i < infoList.Count; i++)
        //    {
        //        var isReached = false;

        //        InstantiateCard(true,
        //            false, 
        //            infoList[i].Rank, 
        //            infoList[i].Suit, 
        //            _communityTableContainer, 
        //            out var cardView, 
        //            () => isReached = true);

        //        _cardViewList_onCommunity.Add(cardView);

        //        yield return new WaitUntil(() => isReached);
        //    }

        //    _isBankerDealingCardsForCommunity = false;
        //}
        //

        /// <summary>
        /// This function ay mag instantiate ng card at mag animate papuntang destination point
        /// </summary>
        /// <param name="isOpenCard"></param>
        /// <param name="useRotation"></param>
        /// <param name="rank"></param>
        /// <param name="suit"></param>
        /// <param name="cardViewParent"></param>
        /// <param name="cardView"></param>
        /// <param name="isReachedCallback"></param>
        //void InstantiateCard(bool isOpenCard, bool useRotation, GlobalHawk.Rank rank, GlobalHawk.Suit suit, Transform cardViewParent, out CardView cardView, UnityAction isReachedCallback)
        //{
        //    var card = Instantiate(_cardViewPrefab, _deckTableGroup);

        //    cardView = card;

        //    var cardIn = CardLibrary.main.GetCardsInfos().GetCardInfo(rank, suit);
        //    card.InitCarView(cardIn, cardViewParent);

        //    card.transform.position = _bankerPosition.position;
        //    if(useRotation) card.transform.DORotate(new Vector3(0, 0, 120), 0.2f).SetEase(Ease.InOutSine);
        //    card.transform.DOMove(cardViewParent.position, 0.3f)
        //    .SetEase(Ease.InOutSine)
        //    .OnComplete(() =>
        //    {
        //        card.transform.SetParent(cardViewParent);
        //        card.transform.localScale = new Vector2(0.56f, 0.56f);
        //        card.transform.localRotation = Quaternion.Euler(0, 0, 0);
        //        if (isOpenCard) card.OpenCard();
        //        isReachedCallback();
        //    });
        //}

        /// <summary>
        /// This coroutine rereturns lahat ng cards sa banker
        /// </summary>
        /// <returns></returns>
        IEnumerator ReturnAllCardsToBanker()
        {
            var waitSec = new WaitForSeconds(0.1f);

            _cardViewList_onPlayers.ForEach(i => i.CloseCard());
            _cardViewList_onCommunity.ForEach(i => i.CloseCard());

            yield return new WaitForSeconds(0.3f);

            for (int i = 0; i < _cardViewList_onPlayers.Count; i++)
            {
                _cardViewList_onPlayers[i].transform.SetParent(_deckTableGroup);
                _cardViewList_onPlayers[i].transform.localScale = new Vector2(0.56f, 0.56f);
                _cardViewList_onPlayers[i].transform.DOMove(_deckTableGroup.position, 0.25f).SetEase(Ease.InOutSine).OnComplete(null);

                yield return waitSec;
            }

            for (int i = 0; i < _cardViewList_onCommunity.Count; i++)
            {
                _cardViewList_onCommunity[i].transform.SetParent(_deckTableGroup);
                _cardViewList_onCommunity[i].transform.localScale = new Vector2(0.56f, 0.56f);
                _cardViewList_onCommunity[i].transform.DOMove(_deckTableGroup.position, 0.25f).SetEase(Ease.InOutSine).OnComplete(null);

                yield return waitSec;
            }

            for (int i = 0; i < _cardViewList_onPlayers.Count; i++)
            {
                var isReached = false;

                _cardViewList_onPlayers[i].transform.localScale = new Vector2(0.56f, 0.56f);
                _cardViewList_onPlayers[i].transform.DOMove(_bankerPosition.position, 0.03f).SetEase(Ease.InOutSine).OnComplete(() =>
                {
                    isReached = true;
                    Destroy(_cardViewList_onPlayers[i].gameObject, 0.3f);
                });

                yield return new WaitUntil(() => isReached);
            }

            for (int i = 0; i < _cardViewList_onCommunity.Count; i++)
            {
                var isReached = false;

                _cardViewList_onCommunity[i].transform.localScale = new Vector2(0.56f, 0.56f);
                _cardViewList_onCommunity[i].transform.DOMove(_bankerPosition.position, 0.22f).SetEase(Ease.InOutSine).OnComplete(() =>
                {
                    isReached = true;
                    Destroy(_cardViewList_onCommunity[i].gameObject, 0.3f);
                });

                yield return new WaitUntil(() => isReached);
            }

            _cardViewList_onPlayers.Clear();
            _cardViewList_onCommunity.Clear();
            _communityCardsRecords.Clear();

            _isBankerDealsCardsForPlayers = false;
            _isBankerDealingCardsForCommunity = false;
        }

        void OnMessage(MsgType type, IMessage msg)
        {
            Debug.Log($"<color=orange> OnMessage | {type}</color>");
            if (type == MsgType.TableSnapshot)
            {
                Debug.Log($"<color=orange> OnMessage TableSnapshot </color>");
                //Debug.Log($"<color=green> TableSnapshot </color>");
                // Full table state (seats, stacks, pot, board).
                var m = (TableSnapshot)msg;
                // Data you can use:
                // - m.State (waiting/preflop/flop/turn/river/showdown/reset)
                // - m.CommunityCards (board cards if already revealed)
                // - m.PotTotal, m.CurrentBet, m.MinRaise
                // - m.CurrentTurnSeat
                //Debug.Log($"[Snapshot] table={m.TableId} state={m.State} pot={m.PotTotal} currentBet={m.CurrentBet}");

                _debugRoundNameText.text = $"Round Status: {m.State}";

                //foreach (var p in m.Players)
                //{
                //    Debug.Log($"<color=blue>  seat={p.Seat} player={p.PlayerId} stack={p.Stack} bet={p.BetThisRound} status={p.Status} </color>");
                //}
                // Community cards (if any are already revealed)-
                // You can read them like:
                // - m.CommunityCards[0] -> first board card
                // - m.CommunityCards[1] -> second board card
                // If no board yet (pre-flop), count = 0.
                if (m.CommunityCards != null && m.CommunityCards.Count > 0)
                {
                    Debug.Log($"<color=orange>  board={PokerNetConnect.FormatCards(m.CommunityCards)} </color>");
                    StartCoroutine(DealCardsForCommunity(m.CommunityCards));
                }

                if (m.State == TableState.Reset)
                {
                    StartCoroutine(ReturnAllCardsToBanker());
                }
                if (m.State == TableState.PreFlop)
                {
                    //SetBankerDealCards();

                    //StartCoroutine(DealCardsForPlayers());
                }

                if (_playerSeatRecords.Count == 0)
                {
                    for (int i = 0; i < m.Players.Count; i++)
                    {
                        _playerSeatRecords.Add(m.Players[i].PlayerId, i);
                    }
                }
            }

            if (type == MsgType.DealHoleCards)
            {
                // Private hole cards for THIS player only.
                var m = (DealHoleCards)msg;
                // Data you can use:
                // - m.Cards[0..1] : the two hole cards
                // How to read a card:
                // - card.Rank (2-14, where 14 = Ace)
                // - card.Suit (0=Clubs,1=Diamonds,2=Hearts,3=Spades)
                // Example:
                //   var c1 = m.Cards[0];
                //   var c2 = m.Cards[1];
                //   Debug.Log($"card1 rank={c1.Rank} suit={c1.Suit}");
                Debug.Log($"<color=red> [HoleCards] cards={m.Cards} </color>");

                //DealCardsForPlayers(m.Cards, _playerSeatRecords[m.]);
            }

            if (type == MsgType.SpectatorHoleCards)
            {
                var m = (SpectatorHoleCards)msg;
                if (string.IsNullOrEmpty(m.PlayerId)) return;

                //var arr = new Card[2];
                //if (m.Cards.Count > 0) arr[0] = m.Cards[0];
                //if (m.Cards.Count > 1) arr[1] = m.Cards[1];
                //_holeByPlayer[m.PlayerId] = arr;

                //Debug.Log($"[SPEC] {m.PlayerId} hole={FormatCard(arr[0])} {FormatCard(arr[1])}");

                Debug.Log($"<color=red> (12)[HoleCards] cards={m.Cards} </color>");

                DealCardsForPlayers(m.Cards, _playerSeatRecords[m.PlayerId]);
            }
        }

        void InstantiateCard(bool isOpenCard, bool useRotation, GlobalHawk.Rank rank, GlobalHawk.Suit suit, Transform cardViewParent, out CardView cardView, UnityAction isReachedCallback)
        {
            var card = Instantiate(_cardViewPrefab, _deckTableGroup);

            cardView = card;

            var cardIn = CardLibrary.main.GetCardsInfos().GetCardInfo(rank, suit);
            card.InitCarView(cardIn, cardViewParent);

            card.transform.position = _bankerPosition.position;
            if (useRotation) card.transform.DORotate(new Vector3(0, 0, 120), 0.2f).SetEase(Ease.InOutSine);
            card.transform.DOMove(cardViewParent.position, 0.3f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                card.transform.SetParent(cardViewParent);
                card.transform.localScale = new Vector2(0.56f, 0.56f);
                card.transform.localRotation = Quaternion.Euler(0, 0, 0);
                if (isOpenCard) card.OpenCard();
                isReachedCallback();
            });
        }

        IEnumerator DealCardsForCommunity(Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> infoList)
        {
            _isBankerDealingCardsForCommunity = true;

            for (int i = 0; i < infoList.Count; i++)
            {
                if (_communityCardsRecords.ContainsKey(infoList[i])) continue;

                var isReached = false;

                InstantiateCard(true,
                    false,
                    GlobalHawk.TranslateCardRank(infoList[i].Rank),
                    (GlobalHawk.Suit)infoList[i].Suit,
                    _communityTableContainer,
                    out var cardView,
                    () => isReached = true);


                _cardViewList_onCommunity.Add(cardView);
                _communityCardsRecords.Add(infoList[i], cardView);

                yield return new WaitUntil(() => isReached);
            }

            _isBankerDealingCardsForCommunity = false;
        }


        void DealCardsForPlayers(Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> infoList, int seat)
        {
            //_isBankerDealsCardsForPlayers = true;

            for (int j = 0; j < infoList.Count; j++)
            {
               // var isReached = false;

                InstantiateCard(seat == 4,
                    true,
                    GlobalHawk.TranslateCardRank(infoList[j].Rank),
                    (GlobalHawk.Suit)infoList[j].Suit,
                    _playerTablePositions[seat],
                    out var cardView,
                    () =>
                    {

                    });

                _cardViewList_onPlayers.Add(cardView);

               // yield return new WaitUntil(() => isReached);
            }

            //for (int i = 0; i < _playerTablePositions.Length; i++)
            //{
            //    _playerTablePositions[i].transform.localScale = i == 4 ? _playerCardsSizeSets : _otherCardsSizeSets;

              

            //    yield return _drawDelay;
            //}
        }

        /// <summary>
        /// On this coroutine ang banker ay nag bibigay ng mga braha sa each players
        /// </summary>
        /// <param name="infoList"></param>
        /// <returns></returns>
        //IEnumerator DealCardsForPlayers(Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> infoList)
        //{
        //    _isBankerDealsCardsForPlayers = true;

        //    for (int i = 0; i < _playerTablePositions.Length; i++)
        //    {
        //        _playerTablePositions[i].transform.localScale = i == 4 ? _playerCardsSizeSets : _otherCardsSizeSets;

        //        for (int j = 0; j < 2; j++)
        //        {
        //            var isReached = false;

        //            InstantiateCard(i == 4,
        //                true,
        //                GlobalHawk.TranslateCardRank(infoList[i].Rank),
        //                (GlobalHawk.Suit)infoList[i].Suit,
        //                _playerTablePositions[i],
        //                out var cardView,
        //                () => isReached = true);

        //            _cardViewList_onPlayers.Add(cardView);

        //            yield return new WaitUntil(() => isReached);
        //        }

        //        yield return _drawDelay;
        //    }
        //}
    }
}
