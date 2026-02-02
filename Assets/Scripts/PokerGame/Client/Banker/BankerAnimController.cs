////////////////////
//       RECK       //
////////////////////


using NaughtyAttributes;
using System.Threading.Tasks;
using UnityEngine;
using WCC.Core.Exposed;


namespace WCC.Poker.Client
{
    public class BankerAnimController : Exposing<BankerAnimController>
    {
        [SerializeField] Animator _bankerBodyAnimator;
        [SerializeField] Animator _bankerHairAnimator;
        
        readonly static int _dealStyle1 = Animator.StringToHash("DealStyle1");
        readonly static int _dealStyle2 = Animator.StringToHash("DealStyle2");
        readonly static int _windHair = Animator.StringToHash("Wind");
        readonly static int _tip = Animator.StringToHash("Tip");

        [Button]
        public void PlayDealsCardAnimation() => DealingAnimation(_dealStyle1, true);


        [Button]
        public void StopDealsCardAnimation() => DealingAnimation(_dealStyle1, false);

        void DealingAnimation(int key, bool e)
        {
            _bankerBodyAnimator.SetBool(key, e);
            _bankerHairAnimator.SetBool(_windHair, e);
        }

        [Button]
        public async void PlayHappyTip()
        {
            _bankerBodyAnimator.SetTrigger(_tip);
            _bankerHairAnimator.SetBool(_windHair, true);
            await Task.Delay(1000);
            _bankerHairAnimator.SetBool(_windHair, false);
        }


        [Button]
        public void PlayGetCardsAnimation() => DealingAnimation(_dealStyle2, true);

        [Button]
        public void StopGetCardsAnimation() => DealingAnimation(_dealStyle2, false);
    }
}
