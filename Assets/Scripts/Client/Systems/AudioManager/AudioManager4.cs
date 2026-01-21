////////////////////
//       RECK       //
////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Audio;
using WCC.Pocker.Instance;

namespace RD.Utility.Audio
{
    public class AudioManager4 : Exposed<AudioManager4>
    {
        #region VARIABLE-DECLARATIONS
        [SerializeField] AudioMixerGroup _audioMixerGroup;
        [SerializeField] Settings m_Settings;
        [SerializeField] AudioSource _backgroundMusic;
        [SerializeField] List<Library> _library = new();

        AudioSource _audioSource;
        GameObject _audioParent;

        string _lasttKey;
        int _lastElement;

        Func<string, int, Vector3, AudioSettings, bool> _burstState = null;
        readonly Dictionary<string, AudioSource> _soundboard = new();
        AudioClip _lastAudio;
        Vector3 _lastLocation = Vector3.zero;
        Vector3 _positionOffset;

        #endregion VARIABLE-DECLARATIONS

        #region SUB-CLASSES
        [Serializable]
        public class Library
        {
            public string Key;
            public List<AudioClip> AudioClip = new();
        }

        [Serializable]
        public class AudioSettings
        {
            public bool ConnectToSource = true;
            public bool Loop = false;
            [Range(0f, 1f)] public float Blend3dAudio = 0f;
            [Range(0f, 3f)] public float Pitch = 1f;
            [Range(0f, 1f)] public float Volume = 1f;
            [Range(0.1f, 300f)] public float MinDistance = 0.1f;
            [Range(0.1f, 500f)] public float MaxDistance = 500f;
            public VolumeRolloffEnum VolumeRolloff;

            public enum VolumeRolloffEnum
            {
                LogarithmicRolloff, LinearRollof
            }
        }

        [Serializable]
        class Settings
        {
            public bool OptimizeBurst = true;
            public bool IndependentAudio = false;
            public AxisOffset FollowCameraOffset = new();
        }

        [Serializable]
        class AxisOffset
        {
            public bool X;
            public bool Y;
            public bool Z;
        }
        #endregion SUB-CLASSES

        #region MONO
        protected override void Awake()
        {
            base.Awake();
            _audioParent = new GameObject("Soundboard");
            if (!m_Settings.IndependentAudio)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.outputAudioMixerGroup = _audioMixerGroup;
            }
            if (m_Settings.OptimizeBurst) _burstState = SetState;

            if (m_Settings.FollowCameraOffset.X || m_Settings.FollowCameraOffset.Y || m_Settings.FollowCameraOffset.Z)
            {
                var camera = Camera.main.transform;
                var x = m_Settings.FollowCameraOffset.X ? camera.position.x : 0f;
                var y = m_Settings.FollowCameraOffset.Y ? camera.position.y : 0f;
                var z = m_Settings.FollowCameraOffset.Z ? camera.position.z : 0f;
                _positionOffset = new Vector3(x, y, z);
            }
        }
        #endregion

        #region AUDIO-STATE

        bool SetState(string key, int element, Vector3 location, AudioSettings audioSettings)
        {
            if (key == _lasttKey && element == _lastElement && !m_Settings.IndependentAudio)
            {
                StartPlayAudioNonIndependentAudio(_lastAudio, location);
                return true;
            }
            else if (_soundboard.ContainsKey(key + element) && m_Settings.IndependentAudio)
            {
                var audio = _soundboard[key + element];
                ApplySettings(audio, audioSettings);
                StartPlayAudioIndependentAudio(audio, location);
                return true;
            }
            return false;
        }
        #endregion AUDIO-STATE

        #region AUDIO-PLAYER

        public (GameObject, AudioSource) PlayAudio(string key, int element, [Optional] Vector3 position, [Optional] AudioSettings audioSettings)
        {
            GameObject audioGameObject = null;
            AudioSource audioSource = null;
            _lastLocation = position != Vector3.zero ? position : Vector3.zero;
            var localAudioSettings = audioSettings ?? new AudioSettings();
            if (!localAudioSettings.ConnectToSource) (audioGameObject, audioSource) = CreateAudio(key, element, GetAudioClip(key, element), _lastLocation, localAudioSettings);
            if (_burstState?.Invoke(key, element, _lastLocation, localAudioSettings) == true) return (audioGameObject, audioSource);

            var audioClip = GetAudioClip(key, element);

            if (!m_Settings.IndependentAudio) StartPlayAudioNonIndependentAudio(audioClip, _lastLocation);
            else (audioGameObject, audioSource) = CreateAudio(key, element, audioClip, _lastLocation, localAudioSettings);

            _lasttKey = key;
            _lastElement = element;
            _lastAudio = audioClip;

            return (audioGameObject, audioSource);
        }

        public (GameObject, AudioSource) PlayRandomAudio(string key, Vector3 location, [Optional] AudioSettings localAudioSettings)
        {
            GameObject audioGameObject = null;
            AudioSource audioSource = null;
            _library.ForEach(item =>
            {
                if (item.Key == key)
                {
                    (audioGameObject, audioSource) = PlayAudio(key, UnityEngine.Random.Range(0, item.AudioClip.Count), location, localAudioSettings);
                }
            });
            return (audioGameObject, audioSource);
        }
        AudioClip GetAudioClip(string key, int element)
        {
            AudioClip audioClip = null;
            _library.ForEach(item => { if (item.Key == key) audioClip = item.AudioClip[element]; });
            return audioClip;
        }
        #endregion AUDIO-PLAYER

        #region NON-INDIPENDENT-AUDIO
        void StartPlayAudioNonIndependentAudio(AudioClip audioClip, Vector3 location)
        {
            SetPosition(_audioSource.gameObject.transform, location);
            _audioSource.clip = audioClip;
            _audioSource.Play();
        }
        #endregion NON-INDIPENDENT-AUDIO

        #region INDIPENDENT-AUDIO
        void StartPlayAudioIndependentAudio(AudioSource audioSource, Vector3 location)
        {
            SetPosition(audioSource.gameObject.transform, location);
            audioSource.Play();
        }

        (GameObject, AudioSource) CreateAudio(string key, int element, AudioClip audioClip, Vector3 location, AudioSettings audioSettings)
        {
            GameObject audioHolder = new($"{key} - {element} - SoundEffect #{UnityEngine.Random.Range(9999, 100000)}");
            audioHolder.transform.SetParent(_audioParent.transform);
            SetPosition(audioHolder.transform, location);
            var audiosource = audioHolder.AddComponent<AudioSource>();
            audiosource.outputAudioMixerGroup = _audioMixerGroup;
            ApplySettings(audiosource, audioSettings);

            audiosource.clip = audioClip;
            audiosource.Play();

            if (_soundboard.ContainsKey(key + element)) return (audioHolder, audiosource);

            _soundboard.Add(key + element, audiosource);
            return (audioHolder, audiosource);
        }

        void ApplySettings(AudioSource AS, AudioSettings audioSettings)
        {
            if (audioSettings != null)
            {
                AS.loop = audioSettings.Loop;
                AS.volume = audioSettings.Volume;
                AS.pitch = audioSettings.Pitch;
                AS.spatialBlend = audioSettings.Blend3dAudio;
                AS.minDistance = audioSettings.MinDistance;
                AS.maxDistance = audioSettings.MaxDistance;
                AS.rolloffMode = audioSettings.VolumeRolloff == AudioSettings.VolumeRolloffEnum.LinearRollof ? AudioRolloffMode.Linear : AudioRolloffMode.Logarithmic;
            }
        }
        #endregion INDIPENDENT-AUDIO

        #region GAME-BACKGROUND-MUSIC
        public void SetEnableBackgroundMusic(bool play)
        {
            if (play) _backgroundMusic.Play();
            else _backgroundMusic.Stop();
        }
        #endregion GAME-BACKGROUND-MUSIC

        void SetPosition(Transform target, Vector3 location)
        {
            target.position = location + _positionOffset;
        }
    }
}