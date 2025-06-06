using System.Threading.Tasks;
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
        /// Requests a store review asynchronously. This method tracks the number of review requests made
        /// and limits them to a maximum defined threshold. If the request fails, it opens the store page
        /// to encourage manual review.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation of requesting the store review.</returns>
        public static async Task RequestAsync(string storeUrl = null)
        {
            var count = PlayerPrefs.GetInt(ReviewRequestCountPrefKey, 0);
            if (count >= ReviewRequestMaxCount)
                return;

#if UNITY_IOS
            var success = UnityEngine.iOS.Device.RequestStoreReview();
#elif UNITY_ANDROID
            var success = await RequestPlayReviewAsync();
#endif
            PlayerPrefs.SetInt(ReviewRequestCountPrefKey, count + 1);

            if (success == false && string.IsNullOrEmpty(storeUrl) == false)
            {
                Application.OpenURL(storeUrl);
            }
        }

#if UNITY_ANDROID
        private static async Task<bool> RequestPlayReviewAsync()
        {
            var manager = new ReviewManager();
            var requestFlowOperation = await manager.RequestReviewFlow();
            if (requestFlowOperation.Error != ReviewErrorCode.NoError)
                return false;

            var launchOperation = await manager.LaunchReviewFlow(requestFlowOperation);
            return launchOperation.Error == ReviewErrorCode.NoError;
        }
#endif
    }
}