using System;
using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using Google.Play.Review;
#endif

namespace qbot.Utility
{
    public static class StoreReviewRequester
    {
        private const int ReviewRequestMaxCount = 3;
        private static readonly string ReviewRequestCountPrefKey = $"{nameof(StoreReviewRequester)}.{nameof(ReviewRequestCountPrefKey)}";

        /// <summary>
        /// Requests a store review. Limited to a max number of times per app install.
        /// </summary>
        public static void Request()
        {
            var count = PlayerPrefs.GetInt(ReviewRequestCountPrefKey, 0);
            if (count >= ReviewRequestMaxCount)
                return;

#if UNITY_IOS
            var success = UnityEngine.iOS.Device.RequestStoreReview();
            PlayerPrefs.SetInt(ReviewRequestCountPrefKey, count + 1);
#elif UNITY_ANDROID
            var runner = new GameObject("StoreReviewRunner").AddComponent<StoreReviewRunner>();
            runner.Run(() => { PlayerPrefs.SetInt(ReviewRequestCountPrefKey, count + 1); });
#endif
        }
    }

#if UNITY_ANDROID
    public class StoreReviewRunner : MonoBehaviour
    {
        private Action _onComplete;

        public void Run(Action onComplete)
        {
            _onComplete = onComplete;
            StartCoroutine(RequestReviewCoroutine());
        }

        private IEnumerator RequestReviewCoroutine()
        {
            var manager = new ReviewManager();
            var requestFlow = manager.RequestReviewFlow();
            yield return requestFlow;

            if (requestFlow.Error == ReviewErrorCode.NoError)
            {
                var reviewInfo = requestFlow.GetResult();
                var launchFlow = manager.LaunchReviewFlow(reviewInfo);
                yield return launchFlow;
            }

            _onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
#endif
}