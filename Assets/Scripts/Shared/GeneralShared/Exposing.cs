 ////////////////////
//       RECK       //
 ////////////////////

using UnityEngine;

namespace WCC.Poker.Shared.Exposed
{
    public abstract class Exposing<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T main;

        protected virtual void Awake() => main = this as T;
    }
}
