////////////////////
//       RECK       //
////////////////////


using System.Collections.Generic;
using UnityEngine;

namespace WCC.Core.Audio
{
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "WCC/Audio/AudioLibrary")]
    public class AudioData : ScriptableObject
    {

        public List<AudioManager.Library> Infos = new();
    }
}
