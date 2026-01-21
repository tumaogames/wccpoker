////////////////////
//       RECK       //
////////////////////


using System.Collections.Generic;
using UnityEngine;
using static WCC.Poker.Client.Audio.AudioManager;


namespace WCC.Poker.Client.Audio
{
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "WCC/Audio/AudioLibrary")]
    public class AudioData : ScriptableObject
    {

        public List<Library> Infos = new();
    }
}
