using UnityEngine;

namespace Core.Services
{
    public interface IAudioService
    {
        void PlaySFX(string key, Vector3? position = null, float pitch = 1f);
        void PlayMusic(string key, float fadeDuration = 1f);
        void StopMusic(float fadeDuration = 1f);
        void SetMusicVolume(float volume);
        void SetSFXVolume(float volume);
    }
}