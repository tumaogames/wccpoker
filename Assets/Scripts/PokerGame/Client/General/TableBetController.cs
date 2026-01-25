////////////////////
//       RECK       //
////////////////////


using DG.Tweening;
using Google.Protobuf.WellKnownTypes;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using WCC.Poker.Shared;


namespace WCC.Poker.Client
{
    public class TableBetController : MonoBehaviour
    {
        [SerializeField] BetValueUIText _betValueUITextPrefab;
        [SerializeField] GameObject _betValuePlusEffectPrefab;
        [SerializeField] Transform _betGroupContainer;
        [SerializeField] Transform[] _playersBetHolderPositions;
        List<PlayerHUD_UI> _playerHUDList = new();

        [Header("[POT-GROUP]")]
        [SerializeField] TMP_Text _potValueText;
        [SerializeField] GameObject _potHolder;

        readonly Dictionary<int, BetValueUIText> _playerBetDictionary = new();

        int _currentTotalPotValue = 0;

        private void Start() => PlayerHUDController.main.PlayerHUDListListenerEvent += data => _playerHUDList = data;

        [Button]
        public void SetDebug_BetRandomPlayers()
        {
            SetBetPlayer(UnityEngine.Random.Range(50, 2000), UnityEngine.Random.Range(0, _playersBetHolderPositions.Length));
        }

        /// <summary>
        /// This function ay para mag set ng bet sa player using PLAYER ID
        /// </summary>
        /// <param name="betValue"></param>
        /// <param name="playerID"></param>
        public void SetBetPlayer(int betValue, int playerID) => StartBetting(betValue, playerID);

        void StartBetting(int betValue, int playerID)
        {
            if (_playerBetDictionary.ContainsKey(playerID))
            {
                InstantiateBet(_betValuePlusEffectPrefab, _playerHUDList[playerID].transform.position, _playersBetHolderPositions[playerID].position, instance =>
                {
                    instance.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
                    {
                        EditTableBet(betValue, _playerBetDictionary[playerID]);
                        Destroy(instance);
                    });
                });

                return;
            }

            InstantiateBet(_betValueUITextPrefab.gameObject, _playerHUDList[playerID].transform.position, _playersBetHolderPositions[playerID].position, instance =>
            {
                var bvuiT = instance.GetComponent<BetValueUIText>();
                _playerBetDictionary.Add(playerID, bvuiT);
                bvuiT.SetBetValue(betValue);
            });
         
        }

        void InstantiateBet(GameObject prefab, Vector2 startPosition, Vector2 destination, [Optional] UnityAction<GameObject> isReachedCallback)
        {
            var betHolder = Instantiate(prefab, _betGroupContainer);
            betHolder.transform.position = startPosition;
            betHolder.transform.DOMove(destination, 0.3f)
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
            if(!_potHolder.activeInHierarchy) _potHolder.SetActive(true);
            foreach (var bet in _playerBetDictionary)
            {
                bet.Value.SetEnableValueHolder(false);
                bet.Value.transform.DOMove(_potHolder.transform.position, 0.5f)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    _currentTotalPotValue += bet.Value.GetChipValue();
                    _potValueText.text = FormatChips(_currentTotalPotValue);
                    Destroy(bet.Value.gameObject);
                });
            }
            _playerBetDictionary.Clear();
        }


        string FormatChips(int value)
        {
            if (value >= 1_000_000)
                return (value / 1_000f).ToString("0.#") + "M";
            if (value >= 1_000)
                return (value / 1_000f).ToString("0.#") + "K";

            return value.ToString();
        }

    }
}