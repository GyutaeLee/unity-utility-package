using System;
using UnityEngine;

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

        public static void OpenSettings()
        {
            GetStatus(status =>
            {
                switch (status)
                {
                    case Status.Granted:
                    case Status.NotDetermined:
                    case Status.Denied:
#if UNITY_ANDROID
                        OpenAndroidAppSettings();
#elif UNITY_IOS
                        OpenIOSNotificationSettings();
#endif
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(status), status, null);
                }
            });
        }

#if UNITY_ANDROID
        private static void OpenAndroidAppSettings()
        {
            const int FlagActivityNewTask = 0x10000000;

            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var settings = new AndroidJavaClass("android.provider.Settings");

            var intent = new AndroidJavaObject("android.content.Intent", settings.GetStatic<string>("ACTION_APP_NOTIFICATION_SETTINGS"));
            intent.Call<AndroidJavaObject>("addFlags", FlagActivityNewTask);
            intent.Call<AndroidJavaObject>("putExtra", "android.provider.extra.APP_PACKAGE", activity.Call<string>("getPackageName"));

            activity.Call("startActivity", intent);
        }
#endif
    }
}