#if UNITY_EDITOR && (UNITY_IOS || UNITY_VISIONOS)

using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace BreezeSdk.Editor
{
    public static class BreezeXcodePostProcess
    {
        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                IosModifyFrameworks(path);
                IosConfigureDeepLink(path);
            }
        }

        private static void IosModifyFrameworks(string path)
        {
            string projPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromFile(projPath);

            string mainTargetGuid = project.GetUnityMainTargetGuid();

            foreach (var targetGuid in new[] { mainTargetGuid, project.GetUnityFrameworkTargetGuid() })
            {
                project.SetBuildProperty(targetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            }

            project.SetBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

            project.WriteToFile(projPath);
        }

        private static void IosConfigureDeepLink(string path)
        {
            var settings = BreezeEditorSettings.Load();
            string scheme = settings.CleanScheme;

            if (!settings.EnableDeepLinkIos || string.IsNullOrEmpty(scheme))
            {
                Debug.Log("[Breeze] iOS deep link configuration skipped. " +
                    "Set the URL scheme in Breeze > Setup to enable automatic deep link setup.");
                return;
            }

            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            // Add CFBundleURLTypes if not already present for this scheme
            var rootDict = plist.root;
            var urlTypes = rootDict["CFBundleURLTypes"]?.AsArray();
            if (urlTypes == null)
            {
                urlTypes = rootDict.CreateArray("CFBundleURLTypes");
            }

            // Check if scheme already exists
            bool found = false;
            for (int i = 0; i < urlTypes.values.Count; i++)
            {
                var entry = urlTypes.values[i].AsDict();
                var schemes = entry?["CFBundleURLSchemes"]?.AsArray();
                if (schemes != null)
                {
                    for (int j = 0; j < schemes.values.Count; j++)
                    {
                        if (schemes.values[j].AsString() == scheme)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                if (found) break;
            }

            if (!found)
            {
                var urlTypeDict = urlTypes.AddDict();
                urlTypeDict.SetString("CFBundleTypeRole", "Editor");
                urlTypeDict.SetString("CFBundleURLName", PlayerSettings.applicationIdentifier);
                var schemes = urlTypeDict.CreateArray("CFBundleURLSchemes");
                schemes.AddString(scheme);
                Debug.Log($"[Breeze] Added URL scheme \"{scheme}\" to Info.plist");
            }
            else
            {
                Debug.Log($"[Breeze] URL scheme \"{scheme}\" already present in Info.plist");
            }

            plist.WriteToFile(plistPath);
        }
    }
}

#endif
