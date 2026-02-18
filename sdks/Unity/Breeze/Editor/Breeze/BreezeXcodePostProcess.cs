#if UNITY_EDITOR && (UNITY_IOS || UNITY_VISIONOS)

using System.IO;
using System.Text.RegularExpressions;
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
    }
}

#endif
