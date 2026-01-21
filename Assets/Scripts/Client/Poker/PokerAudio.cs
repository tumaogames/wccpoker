 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;


namespace WCC.Poker.Client
{
    public class PokerAudio : MonoBehaviour
    {
        public AudioSource source;
        public AudioClip deal;
        public AudioClip bet;
        public AudioClip win;

        public static PokerAudio I;

        void Awake()
        {
            I = this;
        }

        public void PlayDeal() => source.PlayOneShot(deal);
        public void PlayBet() => source.PlayOneShot(bet);
        public void PlayWin() => source.PlayOneShot(win);
    }
}
