////////////////////
//       RECK       //
////////////////////


using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class FoldEffect : MonoBehaviour
    {
        [SerializeField] Image[] _images;
        [SerializeField] TMP_Text[] _texts;
        [SerializeField] float _fadeDuration = 0.25f;
        [SerializeField, Range(0f, 1f)] float _foldAlpha = 0.6f;
        [SerializeField, Range(0f, 1f)] float _foldDarkenMultiplier = 0.5f;

        Color[] _imageOriginalColors;
        Color[] _textOriginalColors;
        bool _hasCachedOriginals;

        void EnsureOriginalColors()
        {
            if (_hasCachedOriginals &&
                _imageOriginalColors != null &&
                _textOriginalColors != null &&
                _imageOriginalColors.Length == (_images?.Length ?? 0) &&
                _textOriginalColors.Length == (_texts?.Length ?? 0))
                return;

            _imageOriginalColors = new Color[_images?.Length ?? 0];
            for (int i = 0; i < _imageOriginalColors.Length; i++)
                _imageOriginalColors[i] = _images[i] != null ? _images[i].color : Color.white;

            _textOriginalColors = new Color[_texts?.Length ?? 0];
            for (int i = 0; i < _textOriginalColors.Length; i++)
                _textOriginalColors[i] = _texts[i] != null ? _texts[i].color : Color.white;

            _hasCachedOriginals = true;
        }

        public void SetFoldEffect()
        {
            print($"<color=blue>SetFoldEffect at FoldEffect.cs</color>");
            EnsureOriginalColors();
            DOTween.Kill(this);
            for (int i = 0; i < _images.Length; i++)
            {
                var img = _images[i];
                if (img == null) continue;
                var baseColor = _imageOriginalColors[i];
                var target = new Color(baseColor.r * _foldDarkenMultiplier,
                    baseColor.g * _foldDarkenMultiplier,
                    baseColor.b * _foldDarkenMultiplier,
                    baseColor.a * _foldAlpha);
                DOTween.To(() => img.color, c => img.color = c, target, _fadeDuration).SetTarget(this);
            }
            for (int i = 0; i < _texts.Length; i++)
            {
                var text = _texts[i];
                if (text == null) continue;
                var baseColor = _textOriginalColors[i];
                var target = new Color(baseColor.r * _foldDarkenMultiplier,
                    baseColor.g * _foldDarkenMultiplier,
                    baseColor.b * _foldDarkenMultiplier,
                    baseColor.a * _foldAlpha);
                DOTween.To(() => text.color, c => text.color = c, target, _fadeDuration).SetTarget(this);
            }
        }

        public void SetUnfoldEffect()
        {
            print($"<color=blue>SetFoldEffect at SetUnfoldEffect.cs</color>");
            EnsureOriginalColors();
            DOTween.Kill(this);
            for (int i = 0; i < _images.Length; i++)
            {
                var img = _images[i];
                if (img == null) continue;
                var target = _imageOriginalColors[i];
                DOTween.To(() => img.color, c => img.color = c, target, _fadeDuration).SetTarget(this);
            }
            for (int i = 0; i < _texts.Length; i++)
            {
                var text = _texts[i];
                if (text == null) continue;
                var target = _textOriginalColors[i];
                DOTween.To(() => text.color, c => text.color = c, target, _fadeDuration).SetTarget(this);
            }
        }
    }
}
