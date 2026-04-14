#if UNITY_EDITOR

using System;
using System.IO;
using BreezeSdk.Runtime;
using UnityEditor;
using UnityEngine;

namespace BreezeSdk.Editor
{
    [Serializable]
    public class BreezeEditorSettings
    {
        private const string SettingsPath = "ProjectSettings/BreezeSettings.json";

        [SerializeField] private string appScheme = "";
        [SerializeField] private bool enableDeepLinkIos = true;
        [SerializeField] private bool enableDeepLinkAndroid = true;

        public string AppScheme
        {
            get => appScheme;
            set => appScheme = value ?? "";
        }

        public bool EnableDeepLinkIos
        {
            get => enableDeepLinkIos;
            set => enableDeepLinkIos = value;
        }

        public bool EnableDeepLinkAndroid
        {
            get => enableDeepLinkAndroid;
            set => enableDeepLinkAndroid = value;
        }

        /// <summary>
        /// Returns the scheme without trailing "://".
        /// </summary>
        public string CleanScheme
        {
            get
            {
                string s = appScheme ?? "";
                if (s.EndsWith("://"))
                    s = s.Substring(0, s.Length - 3);
                else if (s.EndsWith(":/"))
                    s = s.Substring(0, s.Length - 2);
                else if (s.EndsWith(":"))
                    s = s.Substring(0, s.Length - 1);
                return s.Trim();
            }
        }

        public void Save()
        {
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(SettingsPath, json);
            SyncRuntimeSettings();
        }

        private void SyncRuntimeSettings()
        {
            string dir = BreezeRuntimeSettings.AssetDir;
            if (!AssetDatabase.IsValidFolder(dir))
            {
                // Ensure Assets/Breeze exists
                if (!AssetDatabase.IsValidFolder("Assets/Breeze"))
                    AssetDatabase.CreateFolder("Assets", "Breeze");
                AssetDatabase.CreateFolder("Assets/Breeze", "Resources");
            }

            string path = BreezeRuntimeSettings.AssetPath;
            var asset = AssetDatabase.LoadAssetAtPath<BreezeRuntimeSettings>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BreezeRuntimeSettings>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("appScheme").stringValue = CleanScheme;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        public static BreezeEditorSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return new BreezeEditorSettings();

            try
            {
                string json = File.ReadAllText(SettingsPath);
                var settings = JsonUtility.FromJson<BreezeEditorSettings>(json);
                return settings ?? new BreezeEditorSettings();
            }
            catch
            {
                return new BreezeEditorSettings();
            }
        }
    }
}

#endif
