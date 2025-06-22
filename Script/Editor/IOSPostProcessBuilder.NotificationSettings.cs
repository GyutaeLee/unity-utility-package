using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace qbot.Utility
{
    public static partial class IOSPostProcessBuilder
    {
        // Use a low execution order (999) so this runs after other PostProcessBuild steps
        [PostProcessBuild(999)]
        public static void InjectNative(BuildTarget target, string path)
        {
            // ① Create the .mm source file
            var pluginsDir = Path.Combine(path, "Libraries", "NotificationSettings");
            Directory.CreateDirectory(pluginsDir);

            var mmPath = Path.Combine(pluginsDir, "CRNotificationSettings.mm");
            if (!File.Exists(mmPath) || File.ReadAllText(mmPath) != NativeSource)
            {
                File.WriteAllText(mmPath, NativeSource);
            }

            // ② Add the file to the Xcode project
            var projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);

#if UNITY_2019_3_OR_NEWER
            var targetGuid = proj.GetUnityFrameworkTargetGuid();
#else
            var targetGuid = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif
            var fileGuid = proj.AddFile(mmPath, "CRNotificationSettings.mm");
            proj.AddFileToBuild(targetGuid, fileGuid);
            proj.WriteToFile(projPath);
        }

        private const string NativeSource = @"
#import <UIKit/UIKit.h>

extern ""C"" void OpenIOSNotificationSettings()
{
    if (@available(iOS 16.0, *)) {
        [[UIApplication sharedApplication] openURL:
            [NSURL URLWithString:UIApplicationOpenNotificationSettingsURLString]
                                           options:@{}
                                 completionHandler:nil];
    } else {
        NSURL *url = [NSURL URLWithString:UIApplicationOpenSettingsURLString];
        if ([[UIApplication sharedApplication] canOpenURL:url]) {
            [[UIApplication sharedApplication] openURL:url
                                               options:@{}
                                     completionHandler:nil];
        }
    }
}";
    }
}