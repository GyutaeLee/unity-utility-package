using UnityEngine;

namespace qbot.Utility
{
    public class MonoBehaviourSingleton<T> : MonoBehaviour where T : Component
    {
#region Properties

        public static T Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindObjectOfType<T>();
                if (instance != null)
                {
                    return instance;
                }

                var obj = new GameObject(typeof(T).Name);
                instance = obj.AddComponent<T>();

                return instance;
            }
            set => instance = value;
        }

#endregion

#region Fields

        private static T instance;

#endregion

#region Monobehaviour functions

        protected virtual void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
            }
        }

#endregion
    }
}