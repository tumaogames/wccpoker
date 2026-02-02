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
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WCC.Core.Audio;
using WCC.Poker.Shared;


namespace WCC.Poker.Client
{
    public class DeckCardsController : MonoBehaviour
    {
        [Header("[PREFAB]")]
        [SerializeField] CardView _cardViewPrefab;

        [Header("[CARD]")]
        [SerializeField] float _cardSize = 0.56f;
        [SerializeField] Transform[] _commCardsPosition;

        [Header("[CONTAINERS]")]
        [SerializeField] Transform _deckTableGroup;
        [SerializeField] Transform _bankerPosition;
        [SerializeField] Transform _communityTableContainer;

        [Header("[PLAYER-TABLES]")]
        [SerializeField] Transform[] _playerTablePositions;
        [SerializeField] int _ownerSeatIndex = 4;

        readonly List<CardView> _cardViewList_onPlayers = new();
        readonly List<CardView> _cardViewList_onCommunity = new();

        readonly ConcurrentDictionary<int, CardView> _communityCardsRecords = new();
        readonly ConcurrentDictionary<string, int> _playerSeatRecords = new();
        readonly ConcurrentDictionary<string, PlayerCardsPack> _playerCardsRecords = new();
        readonly List<(string playerId, Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> cards)> _pendingDeals = new();
        TableState _lastTableState = TableState.Unspecified;
        bool _inShowdown = false;
        bool _pendingClearAfterShowdown = false;
        int _ownerSeat = -1;
        bool _requestedRejoinThisHand = false;

        //DEBUG TEXT
        [SerializeField] bool _logDealWarnings = true;
        [SerializeField] bool _logHandSummary = false;
        [SerializeField] bool _logStateTransitions = false;

        public static event Action<string> OnWinnerEvent;
        
        [Serializable]
        class PlayerCardsPack
        {
            public Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> Cards = new();
            public List<CardView> CardViewUI;
            public bool IsPlaceholder;
        }

        [Serializable]
        class PlayerPairCars
        {
            [SerializeField] internal Card[] _cards;
        }

        void Awake() => PokerNetConnect.OnMessageEvent += OnMessage;

        void OnEnable()
        {
            ClearAllCardsImmediate();
            _playerSeatRecords.Clear();
            _pendingDeals.Clear();
            _lastTableState = TableState.Unspecified;
            _inShowdown = false;
            _pendingClearAfterShowdown = false;
            _requestedRejoinThisHand = false;
        }

        #region DEBUG
        [Space(20)]
        public int Debug_communityCardsRecords;
        public int Debug_playerSeatRecords;
        public int Debug_playerCardsRecords;

        public int Debug_cardViewList_onPlayers;
        public int Debug_cardViewList_onCommunity;

        private void Update()
        {
            Debug_communityCardsRecords = _communityCardsRecords.Count;
            Debug_playerSeatRecords = _playerSeatRecords.Count;
            Debug_playerCardsRecords = _playerCardsRecords.Count;

            Debug_cardViewList_onPlayers = _cardViewList_onPlayers.Count;
            Debug_cardViewList_onCommunity = _cardViewList_onCommunity.Count;
        }
        #endregion DEBUG

        /// <summary>
        /// This coroutine rereturns lahat ng cards sa banker
        /// </summary>
        /// <returns></returns>
        IEnumerator ReturnAllCardsToBanker()
        {
            var waitSec = new WaitForSeconds(0.1f);

            _cardViewList_onPlayers.ForEach(i =>
            {
                i.SetCloseCard();
                i.SetShowOutline(false);
                i.SetSleepCard(false);
            });
            _cardViewList_onCommunity.ForEach(i =>
            {
                i.SetCloseCard();
                i.SetShowOutline(false);
                i.SetSleepCard(false);
            });

            yield return new WaitForSeconds(0.3f);

            for (int i = 0; i < _cardViewList_onPlayers.Count; i++)
            {
                _cardViewList_onPlayers[i].transform.SetParent(_deckTableGroup);
                _cardViewList_onPlayers[i].transform.localScale = new Vector2(_cardSize, _cardSize);
                _cardViewList_onPlayers[i].transform.DOMove(_deckTableGroup.position, 0.1f).SetEase(Ease.InOutSine).OnComplete(null);
                AudioManager.main.PlayAudio("Cards", 0);

                yield return null;
            }

            for (int i = 0; i < _cardViewList_onCommunity.Count; i++)
            {
                _cardViewList_onCommunity[i].transform.SetParent(_deckTableGroup);
                _cardViewList_onCommunity[i].transform.localScale = new Vector2(_cardSize, _cardSize);
                _cardViewList_onCommunity[i].transform.DOMove(_deckTableGroup.position, 0.1f).SetEase(Ease.InOutSine).OnComplete(null);

                yield return null;
            }

            for (int i = 0; i < _cardViewList_onPlayers.Count; i++)
            {
                var isReached = false;

                _cardViewList_onPlayers[i].transform.localScale = new Vector2(_cardSize, _cardSize);
                _cardViewList_onPlayers[i].transform.DOMove(_bankerPosition.position, 0.03f).SetEase(Ease.InSine).OnComplete(() =>
                {
                    isReached = true;
                    Destroy(_cardViewList_onPlayers[i].gameObject);
                });
                AudioManager.main.PlayAudio("Cards", 0);
                yield return new WaitUntil(() => isReached);
            }

            for (int i = 0; i < _cardViewList_onCommunity.Count; i++)
            {
                var isReached = false;

                _cardViewList_onCommunity[i].transform.localScale = new Vector2(_cardSize, _cardSize);
                _cardViewList_onCommunity[i].transform.DOMove(_bankerPosition.position, 0.1f).SetEase(Ease.InSine).OnComplete(() =>
                {
                    isReached = true;
                    Destroy(_cardViewList_onCommunity[i].gameObject);
                });

                yield return new WaitUntil(() => isReached);
            }

            _cardViewList_onPlayers.Clear();
            _cardViewList_onCommunity.Clear();
            _communityCardsRecords.Clear();

            _inShowdown = false;
            if (_pendingClearAfterShowdown)
            {
                _pendingClearAfterShowdown = false;
                ClearAllCardsImmediate();
            }
        }

        void OnMessage(MsgType type, IMessage msg)
        {
            switch (type)
            {
                case MsgType.TableSnapshot: OnTableSnapshot((TableSnapshot)msg); break;
                case MsgType.DealHoleCards: OnDealHoleCards((DealHoleCards)msg); break;
                case MsgType.CommunityCards: OnCommunityCards((CommunityCards)msg); break;
                case MsgType.SpectatorHoleCards: OnSpectatorHoleCards((SpectatorHoleCards)msg); break;
                case MsgType.ActionBroadcast: OnActionBroadcast((ActionBroadcast)msg); break;
                case MsgType.HandResult: OnHandResult((HandResult)msg); break;
            }
        }

        void OnActionBroadcast(ActionBroadcast m)
        {
            if (m.Action != PokerActionType.Fold)
                return;

            SetPlayerCardsSleep(m.PlayerId, true);
        }

        void OnTableSnapshot(TableSnapshot m)
        {
            if (_logStateTransitions && _lastTableState != m.State)
                Debug.Log($"TableState transition: {_lastTableState} -> {m.State}");

            if (m.State == TableState.PreFlop && _communityCardsRecords.Count > 0)
                ClearAllCardsImmediate();

            if (m.CommunityCards != null && m.CommunityCards.Count > 0)
            {
                StartCoroutine(DealCardsForCommunity(m.CommunityCards));
            }

            var ownerId = PokerNetConnect.OwnerPlayerID;
            _ownerSeat = -1;
            for (int i = 0; i < m.Players.Count; i++)
            {
                if (!string.IsNullOrEmpty(ownerId) && m.Players[i].PlayerId == ownerId)
                    _ownerSeat = m.Players[i].Seat;
            }
            for (int i = 0; i < m.Players.Count; i++)
            {
                _playerSeatRecords[m.Players[i].PlayerId] = MapSeatToIndex(m.Players[i].Seat);
            }

            if ((m.State == TableState.PreFlop ||
                 m.State == TableState.Flop ||
                 m.State == TableState.Turn ||
                 m.State == TableState.River) &&
                (_lastTableState != m.State || _playerCardsRecords.Count == 0))
            {
                foreach (var p in m.Players)
                {
                    if (p.Status == PlayerStatus.SittingOut ||
                        p.Status == PlayerStatus.Disconnected)
                        continue;

                    if (_playerCardsRecords.ContainsKey(p.PlayerId))
                        continue;

                    if (_playerSeatRecords.TryGetValue(p.PlayerId, out var seat))
                        DealFaceDownCards(p.PlayerId, seat, 2);
                }
            }
            if (_lastTableState != m.State && m.State == TableState.Waiting)
            {
                if (_inShowdown)
                    _pendingClearAfterShowdown = true;
                else
                    ClearAllCardsImmediate();
                _requestedRejoinThisHand = false;
            }
            if (m.State == TableState.Reset)
            {
                if (_inShowdown)
                    _pendingClearAfterShowdown = true;
                else
                    ClearAllCardsImmediate();
                _requestedRejoinThisHand = false;
            }

            _lastTableState = m.State;

            if (m.State == TableState.PreFlop)
                _requestedRejoinThisHand = false;

            TryRequestOwnerRejoin(m);

            for (int i = _pendingDeals.Count - 1; i >= 0; i--)
            {
                var pd = _pendingDeals[i];
                if (_playerSeatRecords.TryGetValue(pd.playerId, out var seat))
                {
                    DealCardsForPlayers(pd.playerId, pd.cards, seat);
                    _pendingDeals.RemoveAt(i);
                }
            }
        }

        void OnCommunityCards(CommunityCards m)
        {
            if (m.Cards != null && m.Cards.Count > 0)
            {
                if (_logHandSummary)
                    Debug.Log($"CommunityCards ({m.Street}): {PokerNetConnect.FormatCards(m.Cards)}");
                StartCoroutine(DealCardsForCommunity(m.Cards));
            }
        }

        void OnDealHoleCards(DealHoleCards m)
        {
            Debug.Log($"<color=purple>OnDealHoleCards: {PokerNetConnect.OwnerPlayerID} </color>");
            if (string.IsNullOrEmpty(PokerNetConnect.OwnerPlayerID)) return;
            //if (_playerSeatRecords.ContainsKey(PokerNetConnect.OwnerPlayerID)) return;
            if (_playerCardsRecords.TryGetValue(PokerNetConnect.OwnerPlayerID, out var existing) && !existing.IsPlaceholder) return;
            if (existing != null && existing.IsPlaceholder) ClearPlayerCards(PokerNetConnect.OwnerPlayerID);
            if (_playerSeatRecords.TryGetValue(PokerNetConnect.OwnerPlayerID, out var seat))
                DealCardsForPlayers(PokerNetConnect.OwnerPlayerID, m.Cards, seat);
            else
            {
                if (_logDealWarnings)
                    Debug.LogWarning($"DealHoleCards before seat known for player {PokerNetConnect.OwnerPlayerID}. Queued.");
                _pendingDeals.Add((PokerNetConnect.OwnerPlayerID, m.Cards));
            }
        }

        void OnSpectatorHoleCards(SpectatorHoleCards m)
        {
            Debug.Log($"<color=purple>OnSpectatorHoleCards: {m.PlayerId} </color>");
            if (string.IsNullOrEmpty(m.PlayerId)) return;
            if (_playerCardsRecords.TryGetValue(m.PlayerId, out var existing) && !existing.IsPlaceholder) return;
            if (existing != null && existing.IsPlaceholder) ClearPlayerCards(m.PlayerId);
            if (_playerSeatRecords.TryGetValue(m.PlayerId, out var seat))
                DealCardsForPlayers(m.PlayerId, m.Cards, seat);
            else
            {
                if (_logDealWarnings)
                    Debug.LogWarning($"SpectatorHoleCards before seat known for player {m.PlayerId}. Queued.");
                _pendingDeals.Add((m.PlayerId, m.Cards));
            }
        }

        async void OnHandResult(HandResult m)
        {
            _inShowdown = true;
            await Task.Delay(1000);

            AudioManager.main.PlayAudio("SFX", 0);

            // Build map: playerId -> hole cards (revealed at showdown)
            var holeByPlayer = new Dictionary<string, Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card>>(StringComparer.Ordinal);
            foreach (var rh in m.RevealedHands)
                holeByPlayer[rh.PlayerId] = rh.HoleCards;

            foreach (var kvp in holeByPlayer)
            {
                if (!_playerCardsRecords.TryGetValue(kvp.Key, out var pack) || pack.CardViewUI == null)
                    continue;

                pack.Cards = kvp.Value;
                pack.IsPlaceholder = false;

                var count = Math.Min(pack.CardViewUI.Count, kvp.Value.Count);
                for (int i = 0; i < count; i++)
                {
                    var cardInfo = CardLibrary.main.GetCardsInfos().GetCardInfo(
                        GlobalHawk.TranslateCardRank(kvp.Value[i].Rank),
                        (GlobalHawk.Suit)kvp.Value[i].Suit);
                    pack.CardViewUI[i].UpdateCardInfo(cardInfo);
                }
            }

            _cardViewList_onPlayers.ForEach(i => i.SetOpenCard());

            foreach (var comCard in _communityCardsRecords)
                comCard.Value.SetSleepCard(true);

            string winnerP_ID = string.Empty;

            foreach (var w in m.Winners)
            {
                var hole = holeByPlayer.TryGetValue(w.PlayerId, out var hc) ? hc : null;

                if (!_playerCardsRecords.ContainsKey(w.PlayerId)) continue;
                PlayerCardsPack listOfCards = _playerCardsRecords[w.PlayerId];

                foreach (var c in w.BestFive)
                {
                    for (int i = 0; i < listOfCards.Cards.Count; i++)
                    {
                        if (listOfCards.Cards[i].Rank == c.Rank && listOfCards.Cards[i].Suit == c.Suit)
                        {
                            listOfCards.CardViewUI[i].SetSleepCard(false);
                            listOfCards.CardViewUI[i].SetShowOutline(true);
                            winnerP_ID = w.PlayerId;
                        }
                    }

                    var key = CardKey(c);
                    if (_communityCardsRecords.TryGetValue(key, out var comView))
                    {
                        comView.SetSleepCard(false);
                        comView.SetShowOutline(true);
                    }
                }
            }

            _playerCardsRecords.Clear();

            await Task.Delay(3000);

            if (winnerP_ID != string.Empty) SetWinner(winnerP_ID);

            await Task.Delay(5000);

            StartCoroutine(ReturnAllCardsToBanker());

        }

        void InstantiateCard(bool isOpenCard, bool useRotation, GlobalHawk.Rank rank, GlobalHawk.Suit suit, Transform cardViewParent, out CardView cardView, float cardSize, UnityAction isReachedCallback)
        {
            var card = Instantiate(_cardViewPrefab, _deckTableGroup);

            AudioManager.main.PlayAudio("Cards", 0);

            cardView = card;

            var cardIn = CardLibrary.main.GetCardsInfos().GetCardInfo(rank, suit);
            card.InitializeCarView(cardIn, cardViewParent);

            card.transform.position = _bankerPosition.position;
            if (GameServerClient.Instance.IsCatchingUp)
            {
                card.transform.SetParent(cardViewParent);
                card.transform.position = cardViewParent.position;
                card.transform.localScale = new Vector2(cardSize, cardSize);
                card.transform.localRotation = Quaternion.Euler(0, 0, 0);
                if (isOpenCard) card.SetOpenCard();
                isReachedCallback();
                return;
            }

            if (useRotation) card.transform.DORotate(new Vector3(0, 0, 120), 0.2f).SetEase(Ease.InOutSine);
            card.transform.DOMove(cardViewParent.position, 0.3f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                card.transform.SetParent(cardViewParent);
                card.transform.localScale = new Vector2(cardSize, cardSize);
                card.transform.localRotation = Quaternion.Euler(0, 0, 0);
                if (isOpenCard) card.SetOpenCard();
                isReachedCallback();
            });
        }

        IEnumerator DealCardsForCommunity(Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> infoList)
        {
            BankerAnimController.main.PlayDealsCardAnimation();
            for (int i = 0; i < infoList.Count; i++)
            {
                var key = CardKey(infoList[i]);
                if (!_communityCardsRecords.ContainsKey(key))
                {

                    var isReached = false;
                    var targetposition = _commCardsPosition[_communityCardsRecords.Count];
                    InstantiateCard(true,
                        false,
                        GlobalHawk.TranslateCardRank(infoList[i].Rank),
                        (GlobalHawk.Suit)infoList[i].Suit,
                        targetposition,
                        out var cardView,
                        1f,
                        () => isReached = true);


                    _cardViewList_onCommunity.Add(cardView);
                    _communityCardsRecords.TryAdd(key, cardView);

                    yield return new WaitUntil(() => isReached);
                }
                yield return null;
            }
            BankerAnimController.main.StopDealsCardAnimation();
        }


        async void DealCardsForPlayers(string playerID, Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> playerCards, int seat)
        {
            if (seat < 0 || seat >= _playerTablePositions.Length)
            {
                if (_logDealWarnings)
                    Debug.LogWarning($"Invalid seat index {seat} for player {playerID}. Cards not dealt.");
                return;
            }

            BankerAnimController.main.PlayDealsCardAnimation();

            AudioManager.main.PlayAudio("Cards", 1);
            List<CardView> _cardList = new();
            var isLocal = playerID == PokerNetConnect.OwnerPlayerID;
            if (_logHandSummary)
                Debug.Log($"DealHoleCards player={playerID} seat={seat} cards={PokerNetConnect.FormatCards(playerCards)} local={isLocal}");
            for (int j = 0; j < playerCards.Count; j++)
            {
                InstantiateCard(isLocal,
                    true,
                    GlobalHawk.TranslateCardRank(playerCards[j].Rank),
                    (GlobalHawk.Suit)playerCards[j].Suit,
                    _playerTablePositions[seat],
                    out var cardView,
                    _cardSize,
                    () => { });

                _cardViewList_onPlayers.Add(cardView);
                _cardList.Add(cardView);
            }

            _playerCardsRecords.TryAdd(playerID, new PlayerCardsPack
            {
                Cards = playerCards,
                CardViewUI = _cardList,
                IsPlaceholder = false,
            });

            await Task.Delay(500);

            BankerAnimController.main.StopDealsCardAnimation();
        }

        void DealFaceDownCards(string playerID, int seat, int count)
        {
            if (seat < 0 || seat >= _playerTablePositions.Length)
            {
                if (_logDealWarnings)
                    Debug.LogWarning($"Invalid seat index {seat} for player {playerID}. Cards not dealt.");
                return;
            }

            AudioManager.main.PlayAudio("Cards", 1);
            List<CardView> _cardList = new();
            var dummyCards = CreatePlaceholderCards(count);

            for (int j = 0; j < dummyCards.Count; j++)
            {
                InstantiateCard(false,
                    true,
                    GlobalHawk.TranslateCardRank(dummyCards[j].Rank),
                    (GlobalHawk.Suit)dummyCards[j].Suit,
                    _playerTablePositions[seat],
                    out var cardView,
                    _cardSize,
                    () => { });

                _cardViewList_onPlayers.Add(cardView);
                _cardList.Add(cardView);
            }

            _playerCardsRecords[playerID] = new PlayerCardsPack
            {
                Cards = dummyCards,
                CardViewUI = _cardList,
                IsPlaceholder = true,
            };
        }

        Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> CreatePlaceholderCards(int count)
        {
            var list = new Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card>();
            for (int i = 0; i < count; i++)
                list.Add(new Com.poker.Core.Card { Rank = 2, Suit = 0 });
            return list;
        }

        void ClearPlayerCards(string playerID)
        {
            if (!_playerCardsRecords.TryRemove(playerID, out var pack) || pack?.CardViewUI == null)
                return;

            foreach (var cv in pack.CardViewUI)
            {
                if (cv == null) continue;
                _cardViewList_onPlayers.Remove(cv);
                Destroy(cv.gameObject);
            }
        }


        void ClearAllCardsImmediate()
        {
            foreach (var cv in _cardViewList_onPlayers)
            {
                if (cv != null) Destroy(cv.gameObject);
            }
            foreach (var cv in _cardViewList_onCommunity)
            {
                if (cv != null) Destroy(cv.gameObject);
            }

            _cardViewList_onPlayers.Clear();
            _cardViewList_onCommunity.Clear();
            _communityCardsRecords.Clear();
            _playerCardsRecords.Clear();
            _pendingDeals.Clear();
        }

        void SetWinner(string playerID) => OnWinnerEvent?.Invoke(playerID);

        static int CardKey(Com.poker.Core.Card card) => (card.Rank << 2) | (card.Suit & 0x3);

        void SetPlayerCardsSleep(string playerID, bool isSleep)
        {
            if (!_playerCardsRecords.TryGetValue(playerID, out var pack) || pack?.CardViewUI == null)
                return;

            foreach (var cv in pack.CardViewUI)
            {
                if (cv == null) continue;
                cv.SetSleepCard(isSleep);
            }
        }

        int MapSeatToIndex(int seat)
        {
            var total = _playerTablePositions.Length;
            if (total == 0)
                return 0;

            var ownerIndex = Mathf.Clamp(_ownerSeatIndex, 0, total - 1);
            if (_ownerSeat <= 0)
                return Mathf.Clamp(seat - 1, 0, total - 1);

            var offset = ownerIndex - (_ownerSeat - 1);
            var mapped = ((seat - 1 + offset) % total + total) % total;
            return mapped;
        }

        void TryRequestOwnerRejoin(TableSnapshot snapshot)
        {
            if (_requestedRejoinThisHand)
                return;

            if (snapshot == null)
                return;

            if (snapshot.State != TableState.Flop &&
                snapshot.State != TableState.Turn &&
                snapshot.State != TableState.River)
                return;

            var ownerId = PokerNetConnect.OwnerPlayerID;
            if (string.IsNullOrEmpty(ownerId))
                return;

            if (!_playerCardsRecords.TryGetValue(ownerId, out var pack) || pack == null || pack.IsPlaceholder)
            {
                _requestedRejoinThisHand = true;
                GameServerClient.SendRejoinStatic(snapshot.TableId);
            }
        }
    }
}
