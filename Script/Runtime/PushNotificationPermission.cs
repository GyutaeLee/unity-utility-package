using System;
using UnityEngine;
using System.Collections;

#if UNITY_IOS && !UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

namespace qbot.Utility
{
    public static class PushNotificationPermission
    {
        public enum Status
        {
            Granted,
            Denied,
            NotDetermined
        }

        /// <summary>
        /// Retrieves the current push notification permission status asynchronously.
        /// </summary>
        /// <param name="onComplete">Callback with the permission status result.</param>
        /// <param name="timeoutSeconds">If the response is delayed, fallbackStatus will be used after this duration (in seconds).</param>
        /// <param name="fallbackStatus">Default status to use in case of timeout or error.</param>
        public static void GetStatus(
            Action<Status> onComplete,
            float timeoutSeconds = 3f,
            Status fallbackStatus = Status.NotDetermined)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // ---- Android (can be checked synchronously) ----------------------
            Status result = Status.Granted;
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                int sdk = version.GetStatic<int>("SDK_INT");

                if (sdk >= 33)
                {
                    using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    int permission = activity.Call<int>(
                        "checkSelfPermission",
                        "android.permission.POST_NOTIFICATIONS");

                    result = permission == 0 ? Status.Granted : Status.Denied;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PushPerm] Exception while checking Android permission: {e}");
                result = fallbackStatus;
            }

            onComplete?.Invoke(result);
            return;
#endif

#if UNITY_IOS && !UNITY_EDITOR
            // ---- iOS (requires async check with timeout fallback) -------------
            bool finished = false;

            iOSNotificationCenter.GetNotificationSettings(settings =>
            {
                if (finished) return;
                finished = true;

                Status res = settings.AuthorizationStatus switch
                {
                    AuthorizationStatus.Authorized      => Status.Granted,
                    AuthorizationStatus.Denied          => Status.Denied,
                    AuthorizationStatus.NotDetermined   => Status.NotDetermined,
                    _                                   => fallbackStatus
                };
                onComplete?.Invoke(res);
            });

            // Start timeout fallback
            CoroutineManager.Instance.StartManagedCoroutine(TimeoutRoutine(timeoutSeconds, () =>
            {
                if (finished) return;
                finished = true;
                Debug.LogWarning("[PushPerm] iOS permission check timed out");
                onComplete?.Invoke(fallbackStatus);
            }));
#else
            // ---- Editor or unsupported platforms ------------------------------
            onComplete?.Invoke(Status.Granted);
#endif
        }

        private static IEnumerator TimeoutRoutine(float seconds, Action onTimeout)
        {
            yield return new WaitForSecondsRealtime(seconds);
            onTimeout?.Invoke();
        }
    }
}