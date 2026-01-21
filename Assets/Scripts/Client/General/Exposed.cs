 ////////////////////
//       RECK       //
 ////////////////////

using UnityEngine;

namespace WCC.Pocker.Instance
{
    public abstract class Exposed<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T main;

        protected virtual void Awake() => main = this as T;
    }
}
