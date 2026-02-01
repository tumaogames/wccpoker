////////////////////
//       RECK       //
////////////////////

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace WCC.Core.Mod
{
    // Token: 0x0200001B RID: 27
    public class SimpleFunction : MonoBehaviour
    {
        // Token: 0x0600008C RID: 140 RVA: 0x00003968 File Offset: 0x00001B68
        private void Start()
        {
            bool isStartOnAwake = this._isStartOnAwake;
            if (isStartOnAwake)
            {
                base.StartCoroutine(this.StartFunction());
            }
        }

        // Token: 0x0600008D RID: 141 RVA: 0x00003990 File Offset: 0x00001B90
        [ContextMenu("Debug Execute Function")]
        public void GoFunction()
        {
            bool flag = !base.gameObject.activeInHierarchy;
            if (!flag)
            {
                base.StartCoroutine(this.StartFunction());
            }
        }

        // Token: 0x0600008E RID: 142 RVA: 0x000039CA File Offset: 0x00001BCA
        private IEnumerator StartFunction()
        {
            yield return new WaitForSeconds(this.GetDuration());
            UnityEvent functionsEvent = this._functionsEvent;
            if (functionsEvent != null)
            {
                functionsEvent.Invoke();
            }
            yield break;
        }

        // Token: 0x0600008F RID: 143 RVA: 0x000039DC File Offset: 0x00001BDC
        private float GetDuration() => (!this._isSetMaxRandomValue) ? this._delayOnStart : UnityEngine.Random.Range(0.1f, this._delayOnStart);


        // Token: 0x0400005F RID: 95
        [SerializeField]
        private bool _isStartOnAwake = false;

        [Header("[DELAY]")]
        // Token: 0x04000060 RID: 96
        [SerializeField]
        private bool _isSetMaxRandomValue = false;

        // Token: 0x04000061 RID: 97
        [SerializeField]
        private float _delayOnStart = 0f;

        [Header("[EVENTS]")]
        // Token: 0x04000062 RID: 98
        [SerializeField]
        private UnityEvent _functionsEvent;
    }
}
