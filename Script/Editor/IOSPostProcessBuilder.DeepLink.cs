#if UNITY_IOS && QBOT_UTILITY_PUSH_NOTIFICATIONS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

namespace qbot.Utility
{
    public static partial class IOSPostProcessBuilder
    {
        private const string UrlScheme = "curveraceApp";
        private const string BundleId = "com.myzy.curverace";

        [PostProcessBuild(999)]
        public static void OnPostProcessBuildDeepLink(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
                return;

            ModifyPlist(pathToBuiltProject);
        }

        private static void ModifyPlist(string path)
        {
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            var root = plist.root;

            var urlTypes = root.CreateArray("CFBundleURLTypes");
            var dict = urlTypes.AddDict();
            dict.SetString("CFBundleURLName", BundleId);
            var schemes = dict.CreateArray("CFBundleURLSchemes");
            schemes.AddString(UrlScheme);

            plist.WriteToFile(plistPath);
        }
    }
}
#endif