////////////////////
//       RECK       //
////////////////////


using NaughtyAttributes;
using UnityEngine;
using WCC.Core.Exposed;


namespace WCC.Poker.Client
{
    public class BankerAnimController : Exposing<BankerAnimController>
    {
        [SerializeField] Animator _bankerBodyAnimator;
        [SerializeField] Animator _bankerHairAnimator;

        [Button]
        public void PlayDealsCardAnimation()
        {
            _bankerBodyAnimator.SetTrigger("Deals");
            _bankerHairAnimator.SetTrigger("Change");
        }

        [Button]
        public void PlayHappyAnimation()
        {

        }
    }
}
