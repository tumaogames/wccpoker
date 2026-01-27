////////////////////
//       RECK       //
////////////////////


using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace WCC.Poker.Client
{
    public class BetValueUIText : MonoBehaviour
    {
        [SerializeField] TMP_Text _betValueText;
        [SerializeField] Image _chipImage;
        [SerializeField] Image _betHolderImage;
        [SerializeField] Color[] _betColors;
        //
        [SerializeField] Sprite[] _chipsSprites;

        int _currentValue;

        #region VALIDATE
        private void OnValidate()
        {
            if (_chipsSprites.Length != 4) _chipsSprites = new Sprite[4];
            if (_betColors.Length != 4) _betColors = new Color[4];
        }
        #endregion VALIDATE

        public void SetBetValue(int value)
        {
            _currentValue = value;
            _betValueText.text = value.ToString();
            _chipImage.sprite = GetSprite(value);
        }

        public void SetChipsIconToHeavy() => _chipImage.sprite = _chipsSprites[_chipsSprites.Length - 1];

        public void SetEnableValueHolder(bool e) => _betHolderImage.gameObject.SetActive(e);

        Sprite GetSprite(int value)
        {
            if (value <= 100)
            {
                _betHolderImage.color = _betColors[0];
                return _chipsSprites[0];
            }
            else if (value > 100 && value <= 1000)
            {
                _betHolderImage.color = _betColors[1];
                return _chipsSprites[1];
            }
            else if (value > 1000 && value <= 5000)
            {
                _betHolderImage.color = _betColors[2];
                return _chipsSprites[2];
            }
            else if (value > 5000)
            {
                _betHolderImage.color = _betColors[3];
                return _chipsSprites[3];
            }
            return _chipsSprites[0];
        }

        public int GetChipValue() => _currentValue;
    }
}
