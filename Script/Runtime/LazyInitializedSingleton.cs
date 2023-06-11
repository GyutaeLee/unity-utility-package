using UnityEngine;

namespace qbot.Utility
{
    public class LazyInitializedSingleton<T> : MonoBehaviour where T : Component
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();
                    if (instance == null)
                    {
                        var obj = new GameObject(typeof(T).Name);
                        instance = obj.AddComponent<T>();
                    }

                    return instance;
                }

                return instance;
            }
            set
            {
                instance = value;
            }
        }
    }
}