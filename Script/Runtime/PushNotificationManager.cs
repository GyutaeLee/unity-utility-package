#if QBOT_UTILITY_PUSH_NOTIFICATIONS
using System;
using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace qbot.Utility
{
    [Serializable]
    public class PushNotificationPayload
    {
        public string Action;
        public string Reward;
    }

    public static class PushNotificationManager
    {
        public static readonly string NotificationActionKey = nameof(PushNotificationPayload.Action).ToLower();
        public static readonly string NotificationRewardIdKey = nameof(PushNotificationPayload.Reward).ToLower();

        public static Action<PushNotificationPayload> OnPushIntentReceived;
        public static PushNotificationPayload LastPushPayload { get; private set; }

        private const string AndroidChannelId = "default";
        private static bool initialized;

#if UNITY_ANDROID
        private static AndroidJavaObject lastIntent;
#endif

        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

#if UNITY_ANDROID
            InitializeAndroid();
            Application.focusChanged += OnAppFocusChanged;
#elif UNITY_IOS
            InitializeIOS();
#endif
        }

#if UNITY_ANDROID
        private static void OnAppFocusChanged(bool hasFocus)
        {
            if (hasFocus == false)
                return;

            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var intent = activity.Call<AndroidJavaObject>("getIntent");
            if (intent == null || intent == lastIntent)
                return;

            var action = intent.Call<string>("getStringExtra", NotificationActionKey);
            var reward = intent.Call<string>("getStringExtra", NotificationRewardIdKey);
            if (string.IsNullOrEmpty(action))
                return;

            lastIntent = intent;
            InvokePushIntent(new PushNotificationPayload { Action = action, Reward = reward });

            intent.Call("removeExtra", NotificationActionKey);
            intent.Call("removeExtra", NotificationRewardIdKey);
        }
#endif

        private static void InvokePushIntent(PushNotificationPayload payload)
        {
            LastPushPayload = payload;
            OnPushIntentReceived?.Invoke(payload);
        }

#if UNITY_ANDROID
        private static void InitializeAndroid()
        {
            const string PostPerm = "android.permission.POST_NOTIFICATIONS";
            if (Permission.HasUserAuthorizedPermission(PostPerm) == false)
            {
                Permission.RequestUserPermission(PostPerm);
            }

            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(AndroidChannelId, "Default", "General", Importance.Default));
            CoroutineManager.Instance.StartManagedCoroutine(WaitAndQueryLastNotificationIntent());
        }

        /// <summary>
        /// Handle cold-launch push intent after a short delay to ensure event subscribers are ready
        /// </summary>
        private static IEnumerator WaitAndQueryLastNotificationIntent()
        {
            yield return null;
            var intent = AndroidNotificationCenter.GetLastNotificationIntent();
            if (intent != null)
            {
                TryParseAndHandlePayload(intent.Notification.IntentData);
            }
        }
#endif

#if UNITY_IOS
        private static void InitializeIOS()
        {
            // Handle push notifications received while app is running (foreground/background)
            iOSNotificationCenter.OnNotificationReceived += OnIosNotificationClicked;

            // Handle cold-launch push notification after one frame to ensure event subscribers are ready
            CoroutineManager.Instance.StartManagedCoroutine(WaitAndQueryLastNotification());
        }

        /// <summary>
        /// Delays cold-launch handler by one frame to ensure OnPushIntentReceived has subscribers
        /// </summary>
        private static IEnumerator WaitAndQueryLastNotification()
        {
            yield return null;
            yield return QueryLastRespondedIosNotification();
        }

        private static void OnIosNotificationClicked(iOSNotification notification)
        {
            if (notification == null)
                return;

            if (notification.UserInfo.TryGetValue(NotificationActionKey, out var action) == false)
                return;

            notification.UserInfo.TryGetValue(NotificationRewardIdKey, out var rewardId);
            OnPushIntentReceived?.Invoke(new PushNotificationPayload { Action = action, Reward = rewardId });
        }

        private static IEnumerator QueryLastRespondedIosNotification()
        {
            var op = iOSNotificationCenter.QueryLastRespondedNotification();
            yield return op;
            var n = op.Notification;
            if (n == null || n.UserInfo.TryGetValue(NotificationActionKey, out var action) == false)
                yield break;

            n.UserInfo.TryGetValue(NotificationRewardIdKey, out var rewardId);
            InvokePushIntent(new PushNotificationPayload { Action = action, Reward = rewardId });
        }
#endif

        public static void SchedulePayloadNotification(string title, string body, int delaySec, PushNotificationPayload notificationPayload)
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.SendNotification(
                new AndroidNotification
                {
                    Title = title,
                    Text = body,
                    FireTime = DateTime.Now.AddSeconds(delaySec),
                    IntentData = JsonUtility.ToJson(notificationPayload)
                }, AndroidChannelId);
#elif UNITY_IOS
            var n = new iOSNotification
            {
                Identifier = Guid.NewGuid().ToString(),
                Title = title,
                Body = body,
                ShowInForeground = true,
                ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
                Trigger = new iOSNotificationTimeIntervalTrigger
                {
                    TimeInterval = TimeSpan.FromSeconds(delaySec),
                    Repeats = false
                }
            };
            n.UserInfo.Add(NotificationActionKey, notificationPayload.Action);
            n.UserInfo.Add(NotificationRewardIdKey, notificationPayload.Reward ?? string.Empty);
            iOSNotificationCenter.ScheduleNotification(n);
#endif
        }

        public static void ScheduleDailyPayloadNotification(int hour, int minute, string title, string body, PushNotificationPayload notificationPayload)
        {
#if UNITY_ANDROID
            var now = DateTime.Now;
            var next = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            if (next < now) next = next.AddDays(1);

            AndroidNotificationCenter.SendNotification(
                new AndroidNotification
                {
                    Title = title,
                    Text = body,
                    FireTime = next,
                    RepeatInterval = TimeSpan.FromDays(1),
                    IntentData = JsonUtility.ToJson(notificationPayload)
                }, AndroidChannelId);
#elif UNITY_IOS
            var n = new iOSNotification
            {
                Identifier = Guid.NewGuid().ToString(),
                Title = title,
                Body = body,
                ShowInForeground = true,
                ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
                Trigger = new iOSNotificationCalendarTrigger
                {
                    Hour = hour,
                    Minute = minute,
                    Repeats = true
                }
            };
            n.UserInfo.Add(NotificationActionKey, notificationPayload.Action);
            n.UserInfo.Add(NotificationRewardIdKey, notificationPayload.Reward ?? string.Empty);
            iOSNotificationCenter.ScheduleNotification(n);
#endif
        }

        public static void CancelAllScheduledNotifications()
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#elif UNITY_IOS
            iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
            Debug.Log("[Notification] All scheduled notifications canceled.");
        }

#if UNITY_ANDROID
        private static void TryParseAndHandlePayload(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var data = JsonUtility.FromJson<PushNotificationPayload>(json);
                if (data != null)
                {
                    InvokePushIntent(data);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Push] Invalid payload: {ex.Message}");
            }
        }
#endif
    }
}
#endif