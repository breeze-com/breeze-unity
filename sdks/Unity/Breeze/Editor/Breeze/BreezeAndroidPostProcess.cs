#if UNITY_EDITOR && UNITY_ANDROID

using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace BreezeSdk.Editor
{
    public class BreezeAndroidPostProcess : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 999;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            Debug.Log("Breeze Post-processing Android project at: " + path);

            string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");

            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("Breeze AndroidManifest.xml not found at: " + manifestPath);
                return;
            }

            try
            {
                XmlDocument manifest = new XmlDocument();
                manifest.Load(manifestPath);

                XmlNamespaceManager nsMgr = new XmlNamespaceManager(manifest.NameTable);
                nsMgr.AddNamespace("android", "http://schemas.android.com/apk/res/android");

                XmlNode applicationNode = manifest.SelectSingleNode("/manifest/application");

                if (applicationNode == null)
                {
                    Debug.LogError("Breeze Could not find <application> node in manifest");
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Breeze Error modifying AndroidManifest.xml: " + e.Message);
            }
        }
    }
}

#endif
