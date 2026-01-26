using System;
using System.Collections.Generic;


namespace Core.Eventing
{
    public static class EventAggregator
    {
        public static readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        /// <summary>
        /// Subscribe specific type of event
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public static void Subscribe<T>(Action<T> callback) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<Delegate>();
            }
            _subscribers[type].Add(callback);
        }
        /// <summary>
        /// Unsubscribe specific type of event
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public static void Unsubscribe<T>(Action<T> callback) where T : IGameEvent
        {
            var type = typeof(T);
            if (_subscribers.ContainsKey(type))
            {
                _subscribers[type].Remove(callback);
            }
        }
        /// <summary>
        /// Publish a new event to all subscribers of the type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameEvent"></param>
        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var list))
            {
                foreach (var subscriber in list)
                {
                    (subscriber as Action<T>)?.Invoke(gameEvent);
                }
            }
        }
        /// <summary>
        /// Clears all event subscriptions. Call during teardown
        /// </summary>
        public static void ClearAll()
        {
            _subscribers.Clear();
        }
    }
}
