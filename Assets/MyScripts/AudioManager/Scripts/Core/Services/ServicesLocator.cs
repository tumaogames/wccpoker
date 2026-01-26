using System;
using System.Collections;
using System.Collections.Generic;


namespace Core.Services
{
    public static class ServicesLocator {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// Registers a service instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                UnityEngine.Debug.LogWarning($"Service of type {type} is already registered. Replacing.");
            }
            _services[type] = service;
        }
        /// <summary>
        /// Get Registered service. Throw if not found
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static T Get<T>() where T : class {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            throw new Exception($"Service of {type} is not registered");
        }
        /// <summary>
        /// Removes a registered service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Unregister<T>() where T : class {
            var type = typeof(T);
            _services.Remove(type);
        }
        /// <summary>
        /// Clear all registered service(for cleanup or scene transitions).
        /// </summary>
        public static void ClearAll() {
            _services.Clear();
        }
    }  
}

