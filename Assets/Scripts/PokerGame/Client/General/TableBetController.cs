////////////////////
//       RECK       //
////////////////////


using DG.Tweening;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WCC.Poker.Shared;


namespace WCC.Poker.Client
{
    public class TableBetController : MonoBehaviour
    {
        [SerializeField] BetValueUIText _betValueUITextPrefab;
        [SerializeField] Transform _betGroupContainer;
        [SerializeField] Transform[] _playersBetHolderPositions;
        List<PlayerHUD_UI> _playerHUDList = new();

        readonly Dictionary<int, BetValueUIText> _playerBetDictionary = new();

        private void Start() => PlayerHUDController.main.PlayerHUDListListenerEvent += OnProfileHUDUpdates;

        void OnProfileHUDUpdates(List<PlayerHUD_UI> data)
        {
            _playerHUDList = data;
        }

        [Button]
        public void SetDebug_BetPlayer_0()
        {
            SetBetPlayer(UnityEngine.Random.Range(50, 2000), UnityEngine.Random.Range(0, _playersBetHolderPositions.Length));
        }

        public void SetBetPlayer(int betValue, int playerID)
        {
            StartBetting(betValue, playerID);
        }

        void StartBetting(int betValue, int playerID)
        {
            if (_playerBetDictionary.ContainsKey(playerID))
            {
                EditTableBet(betValue, _playerBetDictionary[playerID]);
                return;
            }

            InstantiateCard(playerID, betValue, _playerHUDList[playerID].transform.position, _playersBetHolderPositions[playerID].position, out var betText, () =>
            {
                Debug.Log("Is Player Set Bet DONE!");
            });
        }

        //
        void InstantiateCard(int playerID, int betValue, Vector2 startPosition, Vector2 destination, out BetValueUIText betValUIT, UnityAction isReachedCallback)
        {
            var betHolder = Instantiate(_betValueUITextPrefab, _betGroupContainer);
            _playerBetDictionary.Add(playerID, betHolder);
            betValUIT = betHolder;

            betHolder.SetBetValue(betValue);

            betHolder.transform.position = startPosition;
            betHolder.transform.DOMove(destination, 0.3f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                betHolder.transform.localScale = Vector2.one;
                betHolder.transform.localRotation = Quaternion.Euler(0, 0, 0);
                isReachedCallback();
            });
        }

        void EditTableBet(int newBetValue, BetValueUIText betValueUIText)
        {
            betValueUIText.SetBetValue(newBetValue);
        }
    }
}
