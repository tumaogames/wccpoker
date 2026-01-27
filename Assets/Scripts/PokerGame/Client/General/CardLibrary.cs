////////////////////
//       RECK       //
////////////////////

using UnityEngine;
using WCC.Core.Exposed;

namespace WCC.Poker.Client
{
    public class CardLibrary : Exposing<CardLibrary>
    {
        [SerializeField] CardData _cardData;
        
        public CardData GetCardsInfos() => _cardData;
    }
}
