////////////////////
//       RECK       //
////////////////////

using System;
using UnityEngine;

namespace WCC.Poker.Client.Mod
{
    // Token: 0x02000019 RID: 25
    public class Rotator_mod : MonoBehaviour
    {
        // Token: 0x06000083 RID: 131 RVA: 0x00003768 File Offset: 0x00001968
        private void Start()
        {
            float x = this._rotateAxis.X ? 1f : 0f;
            float y = this._rotateAxis.Y ? 1f : 0f;
            float z = this._rotateAxis.Z ? 1f : 0f;
            this._axis = new Vector3(x, y, z);
            bool invert = this._invert;
            if (invert)
            {
                this._axis = -this._axis;
            }
        }

        // Token: 0x06000084 RID: 132 RVA: 0x000037ED File Offset: 0x000019ED
        private void Update()
        {
            this._target.Rotate(this._axis * this._speed);
        }

        // Token: 0x04000054 RID: 84
        [SerializeField]
        private Transform _target;

        // Token: 0x04000055 RID: 85
        [SerializeField]
        private float _speed;

        // Token: 0x04000056 RID: 86
        [SerializeField]
        private bool _invert = false;

        // Token: 0x04000057 RID: 87
        [SerializeField]
        private Rotator_mod.TargetAxis _rotateAxis;

        // Token: 0x04000058 RID: 88
        private Vector3 _axis;

        // Token: 0x02000032 RID: 50
        [Serializable]
        private class TargetAxis
        {
            // Token: 0x040000C4 RID: 196
            public bool X;

            // Token: 0x040000C5 RID: 197
            public bool Y;

            // Token: 0x040000C6 RID: 198
            public bool Z;
        }
    }
}
