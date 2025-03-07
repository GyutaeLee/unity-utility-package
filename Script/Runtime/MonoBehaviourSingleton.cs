using System;
using UnityEngine;

namespace qbot.Utility
{
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : Component
    {
        private static T realInstance;

        private static T MiddleInstance
        {
            get
            {
                OnInstanceCalled?.Invoke();
                return realInstance;
            }
            set => realInstance = value;
        }

        public static T Instance
        {
            get
            {
                if (MiddleInstance != null)
                    return MiddleInstance;

                MiddleInstance = FindAnyObjectByType<T>();
                if (MiddleInstance != null)
                    return MiddleInstance;

                if (typeof(T).IsAbstract)
                    return null;
                
                var obj = new GameObject(typeof(T).Name);
                MiddleInstance = obj.AddComponent<T>();

                return MiddleInstance;
            }
            set => MiddleInstance = value;
        }

        public static event Action OnInstanceCalled;

        protected virtual void Start()
        {
            
        }

        protected virtual void Awake()
        {
            if (MiddleInstance != null)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            OnInstanceCalled = null;
        }
    }
}