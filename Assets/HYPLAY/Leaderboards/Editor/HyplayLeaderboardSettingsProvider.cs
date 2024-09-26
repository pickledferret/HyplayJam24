using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HYPLAY.Leaderboards.Runtime;
using HYPLAY.Runtime;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Hyplay
{
    public class HyplaySettingsProvider
    {
        private static HyplayLeaderboards _settings;
        private static HyplaySettings _app;
        private const string SettingsPath = "Assets/HYPLAY/Leaderboards/Resources/Leaderboards.asset";

        private static VisualElement _root;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Project/HYPLAY/2.Leaderboards", SettingsScope.Project)
            {
                label = "Leaderboards",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = CreateUI,

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "HYPLAYSettings", "Leaderboards", "Leaderboard" }),
            };

            return provider;
        }

        private static void CreateUI(string searchContext, VisualElement rootElement)
        {
            _root = rootElement;
            if (_settings == null)
            {
                _settings = AssetDatabase.LoadAssetAtPath<HyplayLeaderboards>(SettingsPath);
                if (_settings == null)
                {
                    _settings = ScriptableObject.CreateInstance<HyplayLeaderboards>();
                    AssetDatabase.CreateAsset(_settings, SettingsPath);
                }
                _app = Resources.Load<HyplaySettings>("Settings");
                if (_app == null)
                    return;
            }

            if (_app == null || _app.Current == null || string.IsNullOrWhiteSpace(_app.Current.id))
            {
                rootElement.Add(new Label("Please create or select an app from the HYPLAY Settings page"));
                return;
            }

            var settings = new SerializedObject(_settings);
            
            // rootElement is a VisualElement. If you add any children to it, the OnGUI function
            // isn't called because the SettingsProvider uses the UIElements drawing framework.
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/HYPLAY/Leaderboards/Editor/HyplayLeaderboardEditorStyles.uss");
            var doc = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/HYPLAY/Leaderboards/Editor/HyplayLeaderboardEditor.uxml");
            doc.CloneTree(rootElement);
            rootElement.styleSheets.Add(styleSheet);
            rootElement.AddToClassList("body");

            #if NEWTONSOFT_JSON
            _settings.GetLeaderboards(_app.Current.id, _app.Current.secretKey);
            rootElement.Q<Button>("Refresh").clicked += () => _settings.GetLeaderboards(_app.Current.id, _app.Current.secretKey);
            rootElement.Q<Button>("Create").clicked += () => _settings.CreateLeaderboard(_app.Current.id, _app.Current.secretKey);
            rootElement.Bind(settings);
            #endif
        }

        
    }
}