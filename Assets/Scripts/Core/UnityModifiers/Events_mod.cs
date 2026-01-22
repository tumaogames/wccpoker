 ////////////////////
//       RECK       //
 ////////////////////

using UnityEngine;
using UnityEngine.Events;

namespace WCC.Core.Mod
{
    // Token: 0x02000013 RID: 19
    public class Events_mod : MonoBehaviour
    {
        // Token: 0x06000068 RID: 104 RVA: 0x000033C7 File Offset: 0x000015C7
        public void ExecuteEvent(int i)
        {
            this._events[Mathf.Clamp(i, 0, this._events.Length)].Invoke();
        }

        // Token: 0x04000045 RID: 69
        [SerializeField]
        private UnityEvent[] _events;
    }
}
