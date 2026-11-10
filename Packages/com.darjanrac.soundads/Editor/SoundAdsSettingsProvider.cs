using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SoundAds.Editor
{
    /// <summary>Adds <b>Project Settings > SoundAds</b> and the <b>Tools > SoundAds</b> menu.</summary>
    internal static class SoundAdsSettingsProvider
    {
        const string AssetFolder = "Assets/Resources";
        const string AssetPath = AssetFolder + "/" + SoundAdsSettings.ResourcePath + ".asset";

        static string _connectionStatus = string.Empty;
        static UnityWebRequestAsyncOperation _pendingPing;

        [MenuItem("Tools/SoundAds/Create or Select Settings", priority = 0)]
        static void SelectSettings()
        {
            Selection.activeObject = GetOrCreateSettings();
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        [MenuItem("Tools/SoundAds/Open Dashboard", priority = 20)]
        static void OpenDashboard()
        {
            Application.OpenURL(GetOrCreateSettings().serverUrl.TrimEnd('/') + "/dashboard");
        }

        internal static SoundAdsSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SoundAdsSettings>(AssetPath);
            if (settings != null) return settings;

            if (!Directory.Exists(AssetFolder)) Directory.CreateDirectory(AssetFolder);
            settings = ScriptableObject.CreateInstance<SoundAdsSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SoundAds] Created settings at {AssetPath}");
            return settings;
        }

        [SettingsProvider]
        static SettingsProvider Create()
        {
            return new SettingsProvider("Project/SoundAds", SettingsScope.Project)
            {
                label = "SoundAds",
                keywords = new[] { "SoundAds", "ads", "audio", "api key", "monetization" },
                guiHandler = _ =>
                {
                    var settings = GetOrCreateSettings();
                    var serialized = new SerializedObject(settings);

                    EditorGUILayout.LabelField($"SoundAds SDK v{SoundAdsSdk.Version}", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "Settings are stored in " + AssetPath + " and loaded at runtime by SoundAdsSdk.Initialize().",
                        MessageType.None);

                    EditorGUILayout.Space();
                    var property = serialized.GetIterator();
                    property.NextVisible(true); // skip m_Script
                    while (property.NextVisible(false))
                        EditorGUILayout.PropertyField(property, true);
                    serialized.ApplyModifiedProperties();

                    EditorGUILayout.Space();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(_pendingPing != null || settings.testMode))
                        {
                            if (GUILayout.Button("Test connection", GUILayout.Width(140)))
                                Ping(settings.serverUrl);
                        }

                        if (GUILayout.Button("Open dashboard", GUILayout.Width(140)))
                            OpenDashboard();
                    }

                    if (!string.IsNullOrEmpty(_connectionStatus))
                        EditorGUILayout.HelpBox(_connectionStatus,
                            _connectionStatus.StartsWith("OK") ? MessageType.Info : MessageType.Warning);
                }
            };
        }

        static void Ping(string serverUrl)
        {
            var request = UnityWebRequest.Get(serverUrl.TrimEnd('/') + "/health");
            request.timeout = 3;
            _connectionStatus = "Connecting...";
            _pendingPing = request.SendWebRequest();
            _pendingPing.completed += _ =>
            {
                _connectionStatus = request.result == UnityWebRequest.Result.Success
                    ? $"OK: {serverUrl} is reachable"
                    : $"Failed: {request.error}. Start the server with 'dotnet run' in backend/SoundAds.Server.";
                request.Dispose();
                _pendingPing = null;
                SettingsService.RepaintAllSettingsWindow();
            };
        }
    }
}
