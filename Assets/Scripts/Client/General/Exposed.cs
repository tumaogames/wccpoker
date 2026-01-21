 ////////////////////
//       RECK       //
 ////////////////////

using UnityEngine;

namespace WCC.Pocker.Instance
{
    public abstract class Exposed<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Main;

        protected virtual void OnAwake() => Main = this as T;
    }
}
