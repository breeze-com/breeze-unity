#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace BreezeSdk.Editor
{
    public class BreezeSetupWindow : EditorWindow
    {
        private BreezeEditorSettings settings;
        private Vector2 scrollPos;
        private bool showIosSection = true;
        private bool showAndroidSection = true;

        [MenuItem("Tools/Breeze/Setup", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<BreezeSetupWindow>("Breeze Setup");
            window.minSize = new Vector2(400, 300);
        }

        void OnEnable()
        {
            settings = BreezeEditorSettings.Load();
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawHeader();
            EditorGUILayout.Space(8);
            DrawDeepLinkSection();
            EditorGUILayout.Space(8);
            DrawPlatformSections();
            EditorGUILayout.Space(12);
            DrawSaveButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Breeze Payment SDK", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure deep link settings for the Breeze SDK. " +
                "These settings are applied automatically during iOS and Android builds.",
                MessageType.Info);
        }

        private void DrawDeepLinkSection()
        {
            EditorGUILayout.LabelField("Deep Link Configuration", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            settings.AppScheme = EditorGUILayout.TextField(
                new GUIContent("URL Scheme",
                    "The custom URL scheme for your app (e.g. 'yourgame'). " +
                    "Do not include '://' — just the scheme name."),
                settings.AppScheme);

            if (EditorGUI.EndChangeCheck())
            {
                // Strip :// if user pastes it in
                string clean = settings.CleanScheme;
                if (clean != settings.AppScheme)
                    settings.AppScheme = clean;
            }

            string scheme = settings.CleanScheme;
            if (string.IsNullOrEmpty(scheme))
            {
                EditorGUILayout.HelpBox(
                    "URL Scheme is required. Enter your app's custom URL scheme " +
                    "(e.g. 'yourgame').",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Deep link URL: {scheme}://breeze-payment/purchase/success\n" +
                    "Breeze.Initialize() will use this scheme automatically at runtime.",
                    MessageType.None);
            }
        }

        private void DrawPlatformSections()
        {
            showIosSection = EditorGUILayout.Foldout(showIosSection, "iOS Settings", true, EditorStyles.foldoutHeader);
            if (showIosSection)
            {
                EditorGUI.indentLevel++;
                settings.EnableDeepLinkIos = EditorGUILayout.Toggle(
                    new GUIContent("Configure Info.plist",
                        "Automatically add CFBundleURLTypes to Info.plist during iOS builds."),
                    settings.EnableDeepLinkIos);

                if (settings.EnableDeepLinkIos && !string.IsNullOrEmpty(settings.CleanScheme))
                {
                    EditorGUILayout.HelpBox(
                        "The post-build process will add a CFBundleURLTypes entry " +
                        $"with scheme \"{settings.CleanScheme}\" to Info.plist.",
                        MessageType.None);
                }
                EditorGUI.indentLevel--;
            }

            showAndroidSection = EditorGUILayout.Foldout(showAndroidSection, "Android Settings", true, EditorStyles.foldoutHeader);
            if (showAndroidSection)
            {
                EditorGUI.indentLevel++;
                settings.EnableDeepLinkAndroid = EditorGUILayout.Toggle(
                    new GUIContent("Configure AndroidManifest.xml",
                        "Automatically add an intent-filter for the deep link scheme " +
                        "to AndroidManifest.xml during Android builds."),
                    settings.EnableDeepLinkAndroid);

                if (settings.EnableDeepLinkAndroid && !string.IsNullOrEmpty(settings.CleanScheme))
                {
                    EditorGUILayout.HelpBox(
                        "The post-build process will add an intent-filter with scheme " +
                        $"\"{settings.CleanScheme}\" and host \"breeze-payment\" " +
                        "to AndroidManifest.xml.",
                        MessageType.None);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawSaveButton()
        {
            if (GUILayout.Button("Save Settings", GUILayout.Height(30)))
            {
                settings.Save();
                Debug.Log($"[Breeze] Settings saved. URL Scheme: \"{settings.CleanScheme}\"");
            }
        }
    }
}

#endif
