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

        [Button]
        public void PlayDealsCardAnimation() => DealingAnimation(true);


        [Button]
        public void StopDealsCardAnimation() => DealingAnimation(false);

        void DealingAnimation(bool e)
        {
            _bankerBodyAnimator.SetBool("DealStyle1", e);
            _bankerHairAnimator.SetBool("Wind", e);
        }

        [Button]
        public async void PlayHappyTip()
        {
            _bankerBodyAnimator.SetTrigger("Tip");
            _bankerHairAnimator.SetBool("Wind", true);
            await Task.Delay(1000);
            _bankerHairAnimator.SetBool("Wind", false);
        }

    }
}
