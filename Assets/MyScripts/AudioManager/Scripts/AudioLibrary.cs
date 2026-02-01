using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Library", fileName = "AudioLibrary")]
public class AudioLibrary : ScriptableObject
{
    [Serializable]
    public struct ClipEntry
    {
        public string key;
        public AudioChannel channel;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume;
        public bool loop;
    }

    public List<ClipEntry> clips = new List<ClipEntry>();
    private Dictionary<string, ClipEntry> lookup;
    public void Initialize()
    {
        lookup = new Dictionary<string, ClipEntry>();
        foreach (var entry in clips)
        {
            if (!lookup.ContainsKey(entry.key))
                lookup.Add(entry.key, entry);
        }
    }
    public bool TryGetClip(string key, out ClipEntry entry) {
        if (lookup == null) Initialize();
        return lookup.TryGetValue(key, out entry);
    }
}
