#if UNITY_EDITOR && UNITY_ANDROID

using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace BreezeSdk.Editor
{
    public class BreezeAndroidPostProcess : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 999;

        private const string AndroidNs = "http://schemas.android.com/apk/res/android";

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            Debug.Log("[Breeze] Post-processing Android project at: " + path);

            string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");

            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("[Breeze] AndroidManifest.xml not found at: " + manifestPath);
                return;
            }

            var settings = BreezeEditorSettings.Load();
            string scheme = settings.CleanScheme;

            if (!settings.EnableDeepLinkAndroid || string.IsNullOrEmpty(scheme))
            {
                Debug.Log("[Breeze] Android deep link configuration skipped. " +
                    "Set the URL scheme in Breeze > Setup to enable automatic deep link setup.");
                return;
            }

            try
            {
                XmlDocument manifest = new XmlDocument();
                manifest.PreserveWhitespace = true;
                manifest.Load(manifestPath);

                XmlNamespaceManager nsMgr = new XmlNamespaceManager(manifest.NameTable);
                nsMgr.AddNamespace("android", AndroidNs);

                // Find the main Unity activity
                XmlNode activityNode = manifest.SelectSingleNode(
                    "/manifest/application/activity[intent-filter/action[@android:name='android.intent.action.MAIN']]",
                    nsMgr);

                if (activityNode == null)
                {
                    Debug.LogWarning("[Breeze] Could not find main activity in AndroidManifest.xml");
                    return;
                }

                // Check if a Breeze deep link intent-filter already exists
                string xpath = $"intent-filter[data[@android:scheme='{scheme}' and @android:host='breeze-payment']]";
                XmlNode existing = activityNode.SelectSingleNode(xpath, nsMgr);

                if (existing != null)
                {
                    Debug.Log($"[Breeze] Deep link intent-filter for \"{scheme}\" already present in manifest");
                    return;
                }

                // Add the deep link intent-filter
                XmlElement intentFilter = manifest.CreateElement("intent-filter");

                XmlElement action = manifest.CreateElement("action");
                action.SetAttribute("name", AndroidNs, "android.intent.action.VIEW");
                intentFilter.AppendChild(action);

                XmlElement categoryDefault = manifest.CreateElement("category");
                categoryDefault.SetAttribute("name", AndroidNs, "android.intent.category.DEFAULT");
                intentFilter.AppendChild(categoryDefault);

                XmlElement categoryBrowsable = manifest.CreateElement("category");
                categoryBrowsable.SetAttribute("name", AndroidNs, "android.intent.category.BROWSABLE");
                intentFilter.AppendChild(categoryBrowsable);

                XmlElement data = manifest.CreateElement("data");
                data.SetAttribute("scheme", AndroidNs, scheme);
                data.SetAttribute("host", AndroidNs, "breeze-payment");
                intentFilter.AppendChild(data);

                activityNode.AppendChild(intentFilter);

                manifest.Save(manifestPath);
                Debug.Log($"[Breeze] Added deep link intent-filter for \"{scheme}://breeze-payment\" to {manifestPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Breeze] Error modifying AndroidManifest.xml: " + e.Message);
            }
        }
    }
}

#endif
