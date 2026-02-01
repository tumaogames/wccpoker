////////////////////
//       RECK       //
////////////////////

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using WCC.Core.Exposed;

namespace WCC.Core.Audio
{
    public class AudioManager : Exposing<AudioManager>
    {
        #region FIELDS
        [SerializeField] AudioMixerGroup _audioMixerGroup;
        [SerializeField] Settings m_Settings;
        [SerializeField] AudioSource _backgroundMusic;
        [SerializeField] AudioData _libraryInfo;

        AudioSource _sharedSource;
        GameObject _audioParent;

        readonly Dictionary<string, List<AudioClip>> _libraryMap = new();
        readonly Dictionary<(string, int), AudioSource> _soundboard = new();
        readonly Queue<AudioSource> _pool = new();

        string _lastKey;
        int _lastElement;
        AudioClip _lastAudio;
        Vector3 _positionOffset;
        #endregion

        #region SUB
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
            public bool Loop;
            [Range(0f, 1f)] public float Blend3dAudio;
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
        #endregion

        #region MONO
        protected override void Awake()
        {
            base.Awake();

            _audioParent = new GameObject("Soundboard");

            if (!m_Settings.IndependentAudio)
            {
                _sharedSource = gameObject.AddComponent<AudioSource>();
                _sharedSource.outputAudioMixerGroup = _audioMixerGroup;
            }

            // Build fast lookup table
            foreach (var lib in _libraryInfo.Infos)
                _libraryMap[lib.Key] = lib.AudioClip;

            if (Camera.main != null)
            {
                var cam = Camera.main.transform;
                _positionOffset = new Vector3(
                    m_Settings.FollowCameraOffset.X ? cam.position.x : 0,
                    m_Settings.FollowCameraOffset.Y ? cam.position.y : 0,
                    m_Settings.FollowCameraOffset.Z ? cam.position.z : 0
                );
            }
        }

        /// <summary>
        /// Para ito sa pag load automatically galing sa Resources folders
        /// </summary>
        private void Reset()
        {
            if (_libraryInfo == null)
                _libraryInfo = Resources.Load<AudioData>("AudioLibrary");
        }
        #endregion

        #region PUBLIC API
        /// <summary>
        /// This function ay para sa pag Play ng Audio
        /// Itong function na ito ay yung need gamitin sa ibang scripts
        /// </summary>
        /// <param name="key"></param>
        /// <param name="element"></param>
        /// <param name="position"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public (GameObject, AudioSource) PlayAudio(
            string key,
            int element,
            Vector3 position = default,
            AudioSettings settings = null)
        {
            settings ??= new AudioSettings();

            if (!TryGetClip(key, element, out var clip))
            {
                Debug.LogWarning($"[AudioManager] Missing clip {key}:{element}");
                return (null, null);
            }

            // Burst optimization
            if (m_Settings.OptimizeBurst &&
                key == _lastKey &&
                element == _lastElement &&
                !m_Settings.IndependentAudio)
            {
                PlayShared(clip, position);
                return (null, _sharedSource);
            }

            _lastKey = key;
            _lastElement = element;
            _lastAudio = clip;

            if (!m_Settings.IndependentAudio)
            {
                ApplySettings(_sharedSource, settings);
                PlayShared(clip, position);
                return (null, _sharedSource);
            }

            return PlayIndependent(key, element, clip, position, settings);
        }

        /// <summary>
        /// This function ay para mag play ng random audio
        /// Itong function ang ng ibang scripts para maka pag play
        /// </summary>
        /// <param name="key"></param>
        /// <param name="position"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public (GameObject, AudioSource) PlayRandomAudio(
            string key,
            Vector3 position,
            AudioSettings settings = null)
        {
            if (!_libraryMap.TryGetValue(key, out var list))
                return (null, null);

            int index = UnityEngine.Random.Range(0, list.Count);
            return PlayAudio(key, index, position, settings);
        }
        #endregion

        #region CORE
        /// <summary>
        /// This function ay para try ng get na audio clip at may return na bool
        /// </summary>
        /// <param name="key"></param>
        /// <param name="element"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        bool TryGetClip(string key, int element, out AudioClip clip)
        {
            clip = null;
            if (!_libraryMap.TryGetValue(key, out var list)) return false;
            if (element < 0 || element >= list.Count) return false;
            clip = list[element];
            return clip != null;
        }

        /// <summary>
        /// This function ay para lang mag setup ng audioclips ang position
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="pos"></param>
        void PlayShared(AudioClip clip, Vector3 pos)
        {
            SetPosition(_sharedSource.transform, pos);
            _sharedSource.clip = clip;
            _sharedSource.Play();
        }

        /// <summary>
        /// This function ay para sa pag play ng independent na audio
        /// </summary>
        /// <param name="key"></param>
        /// <param name="element"></param>
        /// <param name="clip"></param>
        /// <param name="pos"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        (GameObject, AudioSource) PlayIndependent(
            string key,
            int element,
            AudioClip clip,
            Vector3 pos,
            AudioSettings settings)
        {
            var id = (key, element);

            if (_soundboard.TryGetValue(id, out var existing))
            {
                ApplySettings(existing, settings);
                SetPosition(existing.transform, pos);
                existing.Play();
                return (existing.gameObject, existing);
            }

            var source = GetFromPool();
            source.clip = clip;
            ApplySettings(source, settings);
            SetPosition(source.transform, pos);
            source.Play();

            _soundboard[id] = source;
            return (source.gameObject, source);
        }
        #endregion

        #region POOL
        /// <summary>
        /// This function ay para mag get galing sa pool
        /// Then return as audioSource component
        /// </summary>
        /// <returns></returns>
        AudioSource GetFromPool()
        {
            AudioSource src;
            if (_pool.Count > 0)
            {
                src = _pool.Dequeue();
                src.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("AudioInstance");
                go.transform.SetParent(_audioParent.transform);
                src = go.AddComponent<AudioSource>();
                src.outputAudioMixerGroup = _audioMixerGroup;
            }
            return src;
        }

        /// <summary>
        /// This function ay para mag return to pool
        /// </summary>
        /// <param name="src"></param>
        void ReturnToPool(AudioSource src)
        {
            src.Stop();
            src.gameObject.SetActive(false);
            _pool.Enqueue(src);
        }
        #endregion

        #region UTILS
        /// <summary>
        /// This function ay para mag apply ng settings audio
        /// </summary>
        /// <param name="a"></param>
        /// <param name="s"></param>
        void ApplySettings(AudioSource a, AudioSettings s)
        {
            a.loop = s.Loop;
            a.volume = s.Volume;
            a.pitch = s.Pitch;
            a.spatialBlend = s.Blend3dAudio;
            a.minDistance = s.MinDistance;
            a.maxDistance = s.MaxDistance;
            a.rolloffMode = s.VolumeRolloff ==
                AudioSettings.VolumeRolloffEnum.LinearRollof
                ? AudioRolloffMode.Linear
                : AudioRolloffMode.Logarithmic;
        }

        /// <summary>
        /// This function ay para mag set ng position para sa audio
        /// </summary>
        /// <param name="t"></param>
        /// <param name="pos"></param>
        void SetPosition(Transform t, Vector3 pos) => t.position = pos + _positionOffset;

        /// <summary>
        /// This function ay para mag set enable ng background music using the parameter bool
        /// If the parameter is TRUE then it will play the background music
        /// Else then it will stop the background music
        /// </summary>
        /// <param name="play"></param>
        public void SetEnableBackgroundMusic(bool play)
        {
            Action act = play ? _backgroundMusic.Play : _backgroundMusic.Stop;
            act();
        }
        #endregion
    }
}