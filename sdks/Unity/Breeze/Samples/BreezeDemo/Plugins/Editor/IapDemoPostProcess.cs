using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace BreezeSdk.BreezeDemo.Editor
{
    public class IapDemoPostProcess
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target == BuildTarget.iOS)
            {

                string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
                PBXProject project = new PBXProject();
                project.ReadFromFile(projectPath);

                string targetGuid = project.GetUnityMainTargetGuid();

                // 1. Copy the .storekit file from Unity to the Xcode project folder
                string fileName = "Products.storekit";
                string sourcePath = Path.Combine(UnityEngine.Application.dataPath, "BreezeDemo/Plugins/iOS", fileName);
                string destPath = Path.Combine(pathToBuiltProject, fileName);
                File.Copy(sourcePath, destPath, true);

                // 2. Add the file to the Xcode project
                string fileGuid = project.AddFile(fileName, fileName, PBXSourceTree.Source);
                project.AddFileToBuild(targetGuid, fileGuid);

                project.WriteToFile(projectPath);

                SetStoreKitConfiguration(pathToBuiltProject, fileName);
            }
        }

        private static void SetStoreKitConfiguration(string pathToBuiltProject, string storekitFileName)
        {
            string schemePath = Path.Combine(
                pathToBuiltProject,
                "Unity-iPhone.xcodeproj",
                "xcshareddata",
                "xcschemes",
                "Unity-iPhone.xcscheme"
            );

            if (!File.Exists(schemePath))
            {
                UnityEngine.Debug.LogWarning(
                    $"StoreKit config not set. Missing scheme file at: {schemePath}"
                );
                return;
            }

            XDocument doc = XDocument.Load(schemePath);
            XElement root = doc.Root;
            if (root == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"StoreKit config not set. Invalid scheme file at: {schemePath}"
                );
                return;
            }

            XNamespace ns = root.Name.Namespace;
            XElement launchAction = root.Descendants(ns + "LaunchAction").FirstOrDefault();
            if (launchAction == null)
            {
                launchAction = new XElement(ns + "LaunchAction");
                root.Add(launchAction);
            }

            XElement storeKitRef = launchAction.Element(ns + "StoreKitConfigurationFileReference");
            if (storeKitRef == null)
            {
                storeKitRef = new XElement(ns + "StoreKitConfigurationFileReference");
                launchAction.Add(storeKitRef);
            }

            storeKitRef.SetAttributeValue("identifier", $"../../{storekitFileName}");
            doc.Save(schemePath);
        }
    }
}