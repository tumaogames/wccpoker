////////////////////
//       RECK       //
////////////////////


using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WCC.Core.Audio;


namespace WCC.Poker.Client
{
    public class ChipsUIVolume : MonoBehaviour
    {
        [SerializeField] TMP_InputField _amountIF;
        [SerializeField] GameObject[] _chipsUIImages;
        [SerializeField] Slider _betSlider;
        [SerializeField] float _sfxCooldown = 0.05f;
        [SerializeField] bool _playSfxOnEnable = false;
       
        int _lastEnabledCount = -1;
        float _nextSfxTime;
        int _currentAmount;

        [SerializeField] int _minimumChips = 0;
        [SerializeField] int _maximumChips = 1000000;

        [SerializeField] AudioManager.AudioSettings _audioSettings;

        public int ChipsValue => _currentAmount;

        public void SetMinMaxChips(int min, int max)
        {
            _minimumChips = min;
            _maximumChips = max;
        }

        private void OnEnable()
        {
            if (_betSlider != null)
            {
                _betSlider.value = 0;
                _betSlider.onValueChanged.AddListener(OnSliderValueChanged);
                OnSliderValueChanged(_betSlider.value);
            }
        }

        private void OnDisable()
        {
            if (_betSlider != null)
            {
                _betSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
            _lastEnabledCount = -1;
        }

        private void OnSliderValueChanged(float value)
        {
            if (_chipsUIImages == null || _chipsUIImages.Length == 0) return;

            float normalized = 0f;
            if (_betSlider != null && _betSlider.maxValue > _betSlider.minValue)
            {
                normalized = Mathf.InverseLerp(_betSlider.minValue, _betSlider.maxValue, value);
            }

            int enabledCount = Mathf.RoundToInt(normalized * _chipsUIImages.Length);
            enabledCount = Mathf.Clamp(enabledCount, 0, _chipsUIImages.Length);

            for (int i = 0; i < _chipsUIImages.Length; i++)
            {
                GameObject chip = _chipsUIImages[i];
                if (chip == null) continue;
                chip.SetActive(i < enabledCount);
            }

            int amount = Mathf.RoundToInt(Mathf.Lerp(_minimumChips, _maximumChips, normalized));
            amount = Mathf.Clamp(amount, Mathf.Min(_minimumChips, _maximumChips), Mathf.Max(_minimumChips, _maximumChips));
            if (_amountIF != null)
            {
                _amountIF.text = amount.ToString();
                _currentAmount = amount;
            }

            bool countChanged = enabledCount != _lastEnabledCount;
            if (countChanged)
            {
                bool allowInitial = _playSfxOnEnable || _lastEnabledCount >= 0;
                if (allowInitial && Time.unscaledTime >= _nextSfxTime)
                {
                    AudioManager.main.PlayRandomAudio("Chips_Bet", Vector2.zero, _audioSettings);
                    _nextSfxTime = Time.unscaledTime + _sfxCooldown;
                }
                _lastEnabledCount = enabledCount;
            }
        }
    }
}
