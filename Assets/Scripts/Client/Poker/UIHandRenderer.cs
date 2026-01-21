////////////////////
//       RECK       //
////////////////////


using System.Collections.Generic;
using UnityEngine;


namespace WCC.Poker.Client
{
    public class UIHandRenderer : MonoBehaviour
    {
        public GameObject cardPrefab;
        public Transform container;

        List<GameObject> cards = new();

        public void Render(List<Card> hand)
        {
            foreach (var c in cards)
                Destroy(c);
            cards.Clear();

            foreach (var card in hand)
            {
                var go = Instantiate(cardPrefab, container);
                go.GetComponent<CardView>().Set(card);
                cards.Add(go);
            }
        }
    }
}