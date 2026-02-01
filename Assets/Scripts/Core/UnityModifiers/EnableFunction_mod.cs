 ////////////////////
//       RECK       //
 ////////////////////


using UnityEngine;
using UnityEngine.Events;


namespace WCC.Core.Mod
{
    public class EnableFunction_mod : MonoBehaviour
    {
        [SerializeField] UnityEvent _onEnabledEvent;

        private void OnEnable() => _onEnabledEvent?.Invoke();

    }
}
