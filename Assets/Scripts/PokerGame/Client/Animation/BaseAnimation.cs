////////////////////
//       RECK       //
////////////////////


using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace WCC.Poker.Client
{
    public abstract class BaseAnimation : MonoBehaviour
    {

        readonly Dictionary<string, Action> _animationsDict = new();

        /// <summary>
        /// This function register the animation
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="action"></param>
        protected void RegisterAnimation(string functionName, Action action) => _animationsDict[functionName] = action;

        /// <summary>
        /// This function ay para mag play ng animation using the function name
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="errorCallback"></param>
        public void PlayAnimation(string functionName, Action errorCallback = null)
        {
            if (_animationsDict.TryGetValue(functionName, out var action)) action?.Invoke();
            else errorCallback?.Invoke();
        }
    }
}
