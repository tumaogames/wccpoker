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

        readonly ConcurrentDictionary<Com.poker.Core.Card, CardView> _communityCardsRecords = new();
        readonly ConcurrentDictionary<string, int> _playerSeatRecords = new();
        readonly ConcurrentDictionary<string, PlayerCardsPack> _playerCardsRecords = new();

        //DEBUG TEXT
        [SerializeField] TMP_Text _debugRoundNameText;

        public static event Action<string> OnWinnerEvent;

        
        [Serializable]
        class PlayerCardsPack
        {
            public Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> Cards = new();
            public List<CardView> CardViewUI;
        }

        [Serializable]
        class PlayerPairCars
        {
            [SerializeField] internal Card[] _cards;
        }

        void Awake() => PokerNetConnect.OnMessageEvent += OnMessage;

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
                _cardViewList_onPlayers[i].transform.localScale = new Vector2(0.56f, 0.56f);
                _cardViewList_onPlayers[i].transform.DOMove(_deckTableGroup.position, 0.1f).SetEase(Ease.InOutSine).OnComplete(null);
                AudioManager.main.PlayAudio("Cards", 0);

                yield return null;
            }

            for (int i = 0; i < _cardViewList_onCommunity.Count; i++)
            {
                _cardViewList_onCommunity[i].transform.SetParent(_deckTableGroup);
                _cardViewList_onCommunity[i].transform.localScale = new Vector2(0.56f, 0.56f);
                _cardViewList_onCommunity[i].transform.DOMove(_deckTableGroup.position, 0.1f).SetEase(Ease.InOutSine).OnComplete(null);

                yield return null;
            }

            for (int i = 0; i < _cardViewList_onPlayers.Count; i++)
            {
                var isReached = false;

                _cardViewList_onPlayers[i].transform.localScale = new Vector2(0.56f, 0.56f);
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

                _cardViewList_onCommunity[i].transform.localScale = new Vector2(0.56f, 0.56f);
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
        }

        void OnMessage(MsgType type, IMessage msg)
        {
            switch (type)
            {
                case MsgType.TableSnapshot: OnTableSnapshot((TableSnapshot)msg); break;
                case MsgType.DealHoleCards: OnDealHoleCards((DealHoleCards)msg); break;
                case MsgType.SpectatorHoleCards: OnSpectatorHoleCards((SpectatorHoleCards)msg); break;
                case MsgType.HandResult: OnHandResult((HandResult)msg); break;
            }
        }

        void OnTableSnapshot(TableSnapshot m)
        {
            _debugRoundNameText.text = $"Round Status: {m.State}";

            if (m.CommunityCards != null && m.CommunityCards.Count > 0)
            {
                StartCoroutine(DealCardsForCommunity(m.CommunityCards));
            }

            if (_playerSeatRecords.Count == 0)
            {
                for (int i = 0; i < m.Players.Count; i++)
                {
                    _playerSeatRecords.TryAdd(m.Players[i].PlayerId, i);
                }
            }
        }

        void OnDealHoleCards(DealHoleCards m)
        {
            if (string.IsNullOrEmpty(PokerNetConnect.OwnerPlayerID)) return;
            DealCardsForPlayers(PokerNetConnect.OwnerPlayerID, m.Cards, _playerSeatRecords[PokerNetConnect.OwnerPlayerID]);
        }

        void OnSpectatorHoleCards(SpectatorHoleCards m)
        {
            if (string.IsNullOrEmpty(m.PlayerId)) return;
            DealCardsForPlayers(m.PlayerId, m.Cards, _playerSeatRecords[m.PlayerId]);
        }

        async void OnHandResult(HandResult m)
        {
            await Task.Delay(1000);

            AudioManager.main.PlayAudio("SFX", 0);

            _cardViewList_onPlayers.ForEach(i => i.SetOpenCard());

            // Build map: playerId -> hole cards (revealed at showdown)
            var holeByPlayer = new Dictionary<string, Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card>>(StringComparer.Ordinal);
            foreach (var rh in m.RevealedHands)
            {
                var r = rh.HoleCards;
                holeByPlayer[rh.PlayerId] = rh.HoleCards;
            }

            foreach (var comCard in _communityCardsRecords)
                comCard.Value.SetSleepCard(true);

            string winnerP_ID = string.Empty;

            foreach (var w in m.Winners)
            {
                var hole = holeByPlayer.TryGetValue(w.PlayerId, out var hc) ? hc : null;

                PlayerCardsPack listOfCards = _playerCardsRecords[w.PlayerId];

                foreach (var c in w.BestFive)
                {
                    for (int i = 0; i < listOfCards.Cards.Count; i++)
                    {
                        if (listOfCards.Cards[i].Rank == c.Rank || listOfCards.Cards[i].Suit == c.Suit)
                        {
                            listOfCards.CardViewUI[i].SetSleepCard(false);
                            listOfCards.CardViewUI[i].SetShowOutline(true);
                            winnerP_ID = w.PlayerId;
                        }
                    }

                    if (_communityCardsRecords.ContainsKey(c))
                    {
                        _communityCardsRecords[c].SetSleepCard(false);
                        _communityCardsRecords[c].SetShowOutline(true);
                    }
                }
            }

            _playerCardsRecords.Clear();

            await Task.Delay(3000);

            SetWinner(winnerP_ID);

            await Task.Delay(5000);

            StartCoroutine(ReturnAllCardsToBanker());

        }


        void InstantiateCard(bool isOpenCard, bool useRotation, GlobalHawk.Rank rank, GlobalHawk.Suit suit, Transform cardViewParent, out CardView cardView, UnityAction isReachedCallback)
        {
            var card = Instantiate(_cardViewPrefab, _deckTableGroup);

            AudioManager.main.PlayAudio("Cards", 0);

            cardView = card;

            var cardIn = CardLibrary.main.GetCardsInfos().GetCardInfo(rank, suit);
            card.InitializeCarView(cardIn, cardViewParent);

            card.transform.position = _bankerPosition.position;
            if (useRotation) card.transform.DORotate(new Vector3(0, 0, 120), 0.2f).SetEase(Ease.InOutSine);
            card.transform.DOMove(cardViewParent.position, 0.3f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                card.transform.SetParent(cardViewParent);
                card.transform.localScale = new Vector2(0.56f, 0.56f);
                card.transform.localRotation = Quaternion.Euler(0, 0, 0);
                if (isOpenCard) card.SetOpenCard();
                isReachedCallback();
            });
        }

        IEnumerator DealCardsForCommunity(Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> infoList)
        {
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
                _communityCardsRecords.TryAdd(infoList[i], cardView);

                yield return new WaitUntil(() => isReached);
            }
        }


        void DealCardsForPlayers(string playerID, Google.Protobuf.Collections.RepeatedField<Com.poker.Core.Card> playerCards, int seat)
        {
            AudioManager.main.PlayAudio("Cards", 1);
            List<CardView> _cardList = new();
            for (int j = 0; j < playerCards.Count; j++)
            {
                InstantiateCard(seat == 4,
                    true,
                    GlobalHawk.TranslateCardRank(playerCards[j].Rank),
                    (GlobalHawk.Suit)playerCards[j].Suit,
                    _playerTablePositions[seat],
                    out var cardView,
                    () => { });

                _cardViewList_onPlayers.Add(cardView);
                _cardList.Add(cardView);
            }

            _playerCardsRecords.TryAdd(playerID, new PlayerCardsPack
            {
                Cards = playerCards,
                CardViewUI = _cardList,
            });
        }

        void SetWinner(string playerID) => OnWinnerEvent?.Invoke(playerID);
    }
}
