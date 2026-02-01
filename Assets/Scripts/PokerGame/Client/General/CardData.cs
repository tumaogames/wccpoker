////////////////////
//       RECK       //
////////////////////


using System;
using UnityEngine;
using WCC.Poker.Shared;


namespace WCC.Poker.Client
{
    [CreateAssetMenu(fileName = "Cards", menuName = "WCC/Card/CardInfo")]
    public class CardData : ScriptableObject
    {
        [SerializeField] CardGroup[] CardInfos;

        [Serializable]
        class CardGroup
        {
            public string CardGroupName;
            public CardsInfo[] CardInfos;
        }

        [Serializable]
        public class CardsInfo
        {
            public string CardName;
            public Sprite CardSprite;
            public Card CardType;
        }


        #region VALIDATE
        private void OnValidate()
        {
            if (CardInfos.Length == 0) CardInfos = new CardGroup[4];
            if (CardInfos.Length > 4) CardInfos = new CardGroup[4];
        }
        #endregion VALIDATE


        public CardsInfo GetCardInfo(GlobalHawk.Rank rank, GlobalHawk.Suit suit) => CardInfos[(int)suit].CardInfos[(int)rank];
    }
}
