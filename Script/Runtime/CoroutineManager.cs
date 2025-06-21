using System.Collections;
using UnityEngine;

namespace qbot.Utility
{
    public class CoroutineManager : MonoBehaviourSingleton<CoroutineManager>
    {
        public Coroutine StartManagedCoroutine(IEnumerator coroutine)
        {
            return StartCoroutine(coroutine);
        }
        
        public void StopManagedCoroutine(IEnumerator coroutine)
        {
            StopCoroutine(coroutine);
        }
    }
}
