 ////////////////////
//       RECK       //
 ////////////////////

using UnityEngine;

namespace WCC.Core.Exposed 
{
    /// <summary>
    /// This class ay para sa Singleton
    /// Sample: public class SampleScript : Exposing<SampleScript> { }
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Exposing<T> : MonoBehaviour where T : MonoBehaviour 
    {
        public static T main;

        protected virtual void Awake() => main = this as T;
    }
}
