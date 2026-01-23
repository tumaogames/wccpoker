////////////////////
//       RECK       //
////////////////////


using System;
using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class PlayerHUD_DesignsHandler : MonoBehaviour
    {

        [SerializeField] Image _frameImage;
        [SerializeField] Design[] _frameDesigns;

        [Serializable]
        class Design
        {
            [SerializeField] internal Sprite _sprite;
            [SerializeField] internal Color _spriteColor = Color.white;
        }

        private void Start()
        {
            var item = _frameDesigns[UnityEngine.Random.Range(0, _frameDesigns.Length)];
            _frameImage.sprite = item._sprite;
            _frameImage.color = item._spriteColor;
        }

    }
}
