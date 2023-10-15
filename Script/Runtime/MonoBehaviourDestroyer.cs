using UnityEngine;

namespace qbot.Utility
{
    public class MonoBehaviourDestroyer : MonoBehaviour
    {
        private void Start()
        {
            Destroy(gameObject);
        }
    }
}
