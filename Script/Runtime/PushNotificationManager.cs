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

        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

#if UNITY_ANDROID
            InitializeAndroid();
#elif UNITY_IOS
            InitializeIOS();
#endif
        }

        private static void InvokePushIntent(PushNotificationPayload payload)
        {
            LastPushPayload = payload;
            OnPushIntentReceived?.Invoke(payload);
        }

#if UNITY_ANDROID
        private static void InitializeAndroid()
        {
            const string postPerm = "android.permission.POST_NOTIFICATIONS";
            if (Permission.HasUserAuthorizedPermission(postPerm) == false)
                Permission.RequestUserPermission(postPerm);

            // channel
            AndroidNotificationCenter.RegisterNotificationChannel(
                new AndroidNotificationChannel(AndroidChannelId, "Default", "General", Importance.Default));

            // cold‑launch click
            var intent = AndroidNotificationCenter.GetLastNotificationIntent();
            if (intent != null)
                TryParseAndHandlePayload(intent.Notification.IntentData);
        }
#endif

#if UNITY_IOS
        private static void InitializeIOS()
        {
            // foreground / background click
            iOSNotificationCenter.OnNotificationReceived += OnIosNotificationClicked;

            // cold‑launch click
            CoroutineManager.Instance.StartManagedCoroutine(WaitAndQueryLastNotification());
        }

        /// <summary>
        /// 콜드런치 핸들러를 한 프레임 뒤로 미루어 OnPushIntentReceived 등록 보장
        /// </summary>
        /// <returns></returns>
        private static IEnumerator WaitAndQueryLastNotification()
        {
            yield return null; // 한 프레임 대기
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
                    InvokePushIntent(data);
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