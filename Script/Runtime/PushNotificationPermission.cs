using System;
using UnityEngine;
using System.Runtime.InteropServices;

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace qbot.Utility
{
    /// <summary>
    /// Cross-platform push-notification permission helper (Android / iOS).
    /// </summary>
    public static class PushNotificationPermission
    {
        public enum Status
        {
            Granted,
            Denied,
            NotDetermined
        }

#if UNITY_IOS
        [DllImport("__Internal")] private static extern void OpenIOSNotificationSettings(); // Implementation is inserted by PostProcessBuild
#endif

        /* ─────────────────────────────── ① CURRENT STATUS ─────────────────────────────── */

        public static void GetStatus(Action<Status> onComplete, Status fallback = Status.NotDetermined)
        {
#if UNITY_ANDROID
            onComplete?.Invoke(CheckAndroidStatus(fallback));
#elif UNITY_IOS
            onComplete?.Invoke(CheckIOSStatus(fallback));
#else
            onComplete?.Invoke(fallback);
#endif
        }

#if UNITY_ANDROID
        private static Status CheckAndroidStatus(Status fallback)
        {
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                var sdk = version.GetStatic<int>("SDK_INT");

                if (sdk < 33) 
                    return Status.Granted;

                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var perm = activity.Call<int>("checkSelfPermission",
                    "android.permission.POST_NOTIFICATIONS");

                return perm == 0 ? Status.Granted : Status.Denied;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PushPerm] Android check failed: {e}");
                return fallback;
            }
        }
#endif

#if UNITY_IOS
        private static Status CheckIOSStatus(Status fallback)
        {
            try
            {
                var s = iOSNotificationCenter.GetNotificationSettings();
                return s.AuthorizationStatus switch
                {
                    AuthorizationStatus.Authorized => Status.Granted,
                    AuthorizationStatus.Denied => Status.Denied,
                    AuthorizationStatus.NotDetermined => Status.NotDetermined,
                    _ => fallback
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PushPerm] iOS check failed: {e}");
                return fallback;
            }
        }
#endif

        /* ─────────────────────────────── ② REQUEST PERMISSION ─────────────────────────────── */

        public static void RequestPermission(Action<Status> onComplete, Status fallback = Status.Denied)
        {
#if UNITY_ANDROID
            RequestAndroidPermission(onComplete, fallback);
#elif UNITY_IOS
            OpenIOSNotificationSettings(); // iOS 16+: notification settings, earlier versions: app settings
            GetStatus(onComplete, fallback); // Re-check status immediately
#else
            onComplete?.Invoke(fallback); // Editor / other platforms
#endif
        }

#if UNITY_ANDROID
        private static void RequestAndroidPermission(Action<Status> cb, Status fallback)
        {
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                int sdk = version.GetStatic<int>("SDK_INT");
                if (sdk >= 33)
                {
                    using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    activity.Call("requestPermissions",
                                  new[] { "android.permission.POST_NOTIFICATIONS" }, 9284);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PushPerm] Android request failed: {e}");
            }

            // Immediately re-check the status since no callback is wired on Android
            GetStatus(cb, fallback);
        }
#endif
    }
}