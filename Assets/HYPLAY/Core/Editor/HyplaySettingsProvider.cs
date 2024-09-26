using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HYPLAY.Runtime;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace UnityEditor.Hyplay
{
    public class HyplaySettingsProvider
    {
        private static HyplaySettings _settings;
        private const string SettingsPath = "Assets/HYPLAY/Core/Resources/Settings.asset";

        private static VisualElement _root;
        private static VisualElement _appList;
        private static VisualElement _currentApp;
        private static Button _createApp;
        private static Button _updateCurrent;
        
        private static SerializedProperty _appName;
        private static SerializedProperty _appDescription;
        private static SerializedProperty _appUrl;
        private static Label _createAppStatus;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            HyplayJSON.InstallPackage();
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Project/HYPLAY/1.Core", SettingsScope.Project)
            {
                label = "Settings",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = CreateUI,

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "HYPLAYSettings" })
            };

            return provider;
        }

        private static void CreateUI(string searchContext, VisualElement rootElement)
        {
            _root = rootElement;
            if (_settings == null)
            {
                _settings = AssetDatabase.LoadAssetAtPath<HyplaySettings>(SettingsPath);
                if (_settings == null)
                {
                    _settings = ScriptableObject.CreateInstance<HyplaySettings>();
                    AssetDatabase.CreateAsset(_settings, SettingsPath);
                }
            }

            var settings = new SerializedObject(_settings);
            
            _appName = settings.FindProperty("appName");
            _appDescription = settings.FindProperty("appDescription");
            _appUrl = settings.FindProperty("appUrl");
            
            // rootElement is a VisualElement. If you add any children to it, the OnGUI function
            // isn't called because the SettingsProvider uses the UIElements drawing framework.
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/HYPLAY/Core/Editor/HyplayEditorStyles.uss");
            var doc = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/HYPLAY/Core/Editor/HyplayEditor.uxml");
            doc.CloneTree(rootElement);
            rootElement.styleSheets.Add(styleSheet);
            rootElement.AddToClassList("body");

            var openProject = rootElement.Q<Button>("GetAccessToken");
            openProject.clicked += OpenAccountSettings;

            _currentApp = rootElement.Q<VisualElement>("AppData");
            BuildCurrentApp(settings.FindProperty("currentApp"));
            
            _createApp = _root.Q<Button>("CreateApp");
            _createApp.clicked += CreateApp;
            _root.Q<Button>("SubmitApp").clicked += CreateApp;

            _appList = new VisualElement();

            _updateCurrent = rootElement.Q<Button>("UpdateApp");
            _updateCurrent.clicked += _settings.UpdateCurrent;

            var findApps = _root.Q<Button>("GetApps");
            findApps.clicked += FindApps;
            
            var accessToken = rootElement.Q<PropertyField>("AccessToken");
            accessToken.BindProperty(settings.FindProperty("accessToken"));
            accessToken.RegisterValueChangeCallback(GetAccessToken);
            
            var devToken = rootElement.Q<PropertyField>("DevToken");
            devToken.BindProperty(settings.FindProperty("devToken"));
            
            var expireTime = rootElement.Q<PropertyField>("ExpireTime");
            expireTime.BindProperty(settings.FindProperty("timeoutHours"));
            
            var usePopup = rootElement.Q<PropertyField>("UsePopup");
            usePopup.BindProperty(settings.FindProperty("usePopup"));
            
            rootElement.Add(_appList);
            _createAppStatus = _root.Q<Label>("AppStatus");
            
            rootElement.Bind(settings);
        }

        private static void BuildCurrentApp(SerializedProperty currentApp)
        {
            object currentVal = null;
            
            #if UNITY_2022_3_OR_NEWER
            currentVal = currentApp.boxedValue;
            #else
            var targetObject = currentApp.serializedObject.targetObject;
            var targetObjectClassType = targetObject.GetType();
            var field = targetObjectClassType.GetField(currentApp.propertyPath, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                currentVal = field.GetValue(targetObject);
            }
            #endif
            
            if (currentVal == null)
            {
                _root.Q<VisualElement>("AppSettings").style.display = DisplayStyle.None;
                return;
            }
            _root.Q<Button>("SubmitApp").style.display = DisplayStyle.None;
            var name = _currentApp.Q<PropertyField>("AppName");
            name.BindProperty(currentApp.FindPropertyRelative("name"));
            var description =_currentApp.Q<PropertyField>("AppDescription");
            description.BindProperty(currentApp.FindPropertyRelative("description"));
            var id =_currentApp.Q<PropertyField>("AppID");
            id.BindProperty(currentApp.FindPropertyRelative("id"));
            id.SetEnabled(false);
            id.style.display = DisplayStyle.Flex;
            var secret =_currentApp.Q<PropertyField>("AppSecret");
            secret.BindProperty(currentApp.FindPropertyRelative("secretKey"));
            secret.SetEnabled(false);
            secret.style.display = DisplayStyle.Flex;
            var url =_root.Q<PropertyField>("AppURL");
            url.style.display = DisplayStyle.Flex;
            _root.Q<PropertyField>("NewURL").style.display = DisplayStyle.None;
            url.BindProperty(currentApp.FindPropertyRelative("url"));
            var redirect =_root.Q<PropertyField>("AppRedirectURIs");
            redirect.BindProperty(currentApp.FindPropertyRelative("redirectUris"));
            redirect.style.display = DisplayStyle.Flex;
            GetImage(currentApp
                .FindPropertyRelative("iconImageAsset")
                .FindPropertyRelative("cdnUrl").stringValue, 
                _root.Q<VisualElement>("AppIcon"));
            
            GetImage(currentApp
                    .FindPropertyRelative("backgroundImageAsset")
                    .FindPropertyRelative("cdnUrl").stringValue, 
                _root.Q<VisualElement>("AppBackground"));

            _root.Q<Button>("EditAppIcon").clicked += EditAppIcon;
            _root.Q<Button>("EditAppBackground").clicked += EditAppBackground;
        }

        private static async void EditAppIcon()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Select Icon", "", new[] { "PNG Files", "png" });
            if (!string.IsNullOrEmpty(path))
            {
                byte[] imageData = await File.ReadAllBytesAsync(path);
                var appIconAsset = await _settings.CreateAsset(imageData);
                var settings = new SerializedObject(_settings);
                
                settings.FindProperty("currentApp").FindPropertyRelative("iconImageAsset")
                    .FindPropertyRelative("id").stringValue = appIconAsset.id;
                settings.FindProperty("currentApp").FindPropertyRelative("iconImageAsset")
                    .FindPropertyRelative("cdnUrl").stringValue = appIconAsset.cdnUrl;
                settings.ApplyModifiedProperties();
                settings.Update();
                _settings.UpdateCurrent();
                GetImage(appIconAsset.cdnUrl, _root.Q<VisualElement>("AppIcon"));
            }
        }

        private static async void EditAppBackground()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Select Background", "", new[] { "PNG Files", "png" });
            if (!string.IsNullOrEmpty(path))
            {
                byte[] imageData = await File.ReadAllBytesAsync(path);
                var appIconAsset = await _settings.CreateAsset(imageData);
                var settings = new SerializedObject(_settings);
                
                settings.FindProperty("currentApp").FindPropertyRelative("backgroundImageAsset")
                    .FindPropertyRelative("id").stringValue = appIconAsset.id;
                settings.FindProperty("currentApp").FindPropertyRelative("backgroundImageAsset")
                    .FindPropertyRelative("cdnUrl").stringValue = appIconAsset.cdnUrl;
                settings.ApplyModifiedProperties();
                settings.Update();
                _settings.UpdateCurrent();
                GetImage(appIconAsset.cdnUrl, _root.Q<VisualElement>("AppBackground"));
            }
        }
        
        private static async void CreateAppIcon()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Select Icon", "", new[] { "PNG Files", "png" });
            if (!string.IsNullOrEmpty(path))
            {
                byte[] imageData = await File.ReadAllBytesAsync(path);
                var appIconAsset = await _settings.CreateAsset(imageData);
                GetImage(appIconAsset.cdnUrl, _root.Q<VisualElement>("AppIcon"));
            }
        }

        private static async void CreateAppBackground()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Select Background", "", new[] { "PNG Files", "png" });
            if (!string.IsNullOrEmpty(path))
            {
                byte[] imageData = await File.ReadAllBytesAsync(path);
                var appIconAsset = await _settings.CreateAsset(imageData);
                GetImage(appIconAsset.cdnUrl, _root.Q<VisualElement>("AppBackground"));
            }
        }
        
        private static async void GetImage(string url, VisualElement element)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            var req = UnityWebRequestTexture.GetTexture(url);
            await req.SendWebRequest();
            var tex = (req.downloadHandler as DownloadHandlerTexture)?.texture;
            element.style.backgroundImage = tex;
            
            
        }
        
        private static async void FindApps()
        {
            var res = await _settings.GetApps();
            foreach (var app in res)
            {
                var btn = new Button()
                {
                    text = app.name
                };
                
                btn.clicked += () =>
                {
                    ShowApp(app);
                };
                
                _appList.Add(btn);
            }
        }

        private static void BuildNewApp()
        {
            _createApp.style.opacity = 1;
            _root.Q<Button>("SubmitApp").style.display = DisplayStyle.Flex;
            var name = _root.Q<PropertyField>("AppName");
            name.BindProperty(_appName);
            name.RegisterValueChangeCallback(AppSettingsChanged);
            var description =_root.Q<PropertyField>("AppDescription");
            description.BindProperty(_appDescription);
            description.RegisterValueChangeCallback(AppSettingsChanged);
            var id =_root.Q<PropertyField>("AppID");
            id.Unbind();
            id.SetEnabled(false);
            id.style.display = DisplayStyle.None;
            var secret =_root.Q<PropertyField>("AppSecret");
            secret.Unbind();
            secret.SetEnabled(false);
            secret.style.display = DisplayStyle.None;
            _root.Q<PropertyField>("AppURL").style.display = DisplayStyle.None;
            var url =_root.Q<PropertyField>("NewURL");
            url.style.display = DisplayStyle.Flex;
            url.BindProperty(_appUrl);
            url.RegisterValueChangeCallback(AppSettingsChanged);
            var redirect =_root.Q<PropertyField>("AppRedirectURIs");
            redirect.Unbind();
            redirect.style.display = DisplayStyle.None;
            _root.Q<Button>("EditAppIcon").clicked -= CreateAppIcon;
            _root.Q<Button>("EditAppBackground").clicked -= CreateAppBackground;
        }

        private static async void CreateApp()
        {
            if (_createApp.style.display.value == DisplayStyle.Flex)
            {
                _createApp.style.display = DisplayStyle.None;
                _updateCurrent.style.display = DisplayStyle.None;
                var settings = new SerializedObject(_settings);
                settings.Update();
                BuildNewApp();
                SetButtonActive();
            } else if (CanSubmitNewApp())
            {
                void OnLogReceived(string condition, string stacktrace, LogType type)
                {
                    if (type == LogType.Exception)
                    {
                        if (condition.Contains("readable"))
                            _createAppStatus.text =
                                "Error creating app, texture is not readable. \nFind your texture in the Project view, and find & enable \"read/write\" in the import settings.";
                        
                    } else if (type == LogType.Error)
                    {
                        if (condition.Contains("compress"))
                            _createAppStatus.text =
                                "Error creating app, texture is compressed. \nFind your texture in the Project view, and change \"compression\" to \"none\" in the import settings.";
                    }
                }
                Application.logMessageReceived += OnLogReceived;

                _createApp.style.display = DisplayStyle.Flex;
                _updateCurrent.style.display = DisplayStyle.Flex;
                _createAppStatus.text = "Uploading app icon";
                
                var icon = _root.Q<VisualElement>("AppIcon").style.backgroundImage;
                var background = _root.Q<VisualElement>("AppBackground").style.backgroundImage;
                
                var appIconAsset = await _settings.CreateAsset(icon.value.texture.EncodeToPNG());
                _createAppStatus.text = "Uploading app background";
                var appBackgroundAsset = await _settings.CreateAsset(background.value.texture.EncodeToPNG());
                _createAppStatus.text = "Creating app on HYPLAY backend";
                var redirect = _appUrl.stringValue;
                if (!redirect.EndsWith("/"))
                    redirect += "/";
                redirect += "redirect.html";

                var body = new Dictionary<string, object>()
                {
                    { "iconImageAssetId", appIconAsset.id },
                    { "backgroundImageAssetId", appBackgroundAsset.id },
                    { "name", _appName.stringValue },
                    { "description", _appDescription.stringValue },
                    {
                        "redirectUris", new []
                        {
                            redirect,
                            "myapp://token"
                        }
                    },
                    { "url", _appUrl.stringValue }
                };

                #if UNITY_2022_1_OR_NEWER
                using var req = UnityWebRequest.Post("https://api.hyplay.com/v1/apps", HyplayJSON.Serialize(body), "application/json");
                #else
                using var req = UnityWebRequest.Post("https://api.hyplay.com/v1/apps", "");
                HyplayJSON.SetData(ref req, HyplayJSON.Serialize(body));
                #endif
                
                req.SetRequestHeader("x-authorization", _settings.AccessToken);
                await req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var settings = new SerializedObject(_settings);
                    _createAppStatus.text = "Created app :)";
                    _settings.SetCurrent(HyplayJSON.Deserialize<HyplayApp>(req.downloadHandler.text));
                    
                    settings.Update();
                    settings.ApplyModifiedProperties();
                    BuildCurrentApp(settings.FindProperty("currentApp"));
                }
                else
                {
                    _createAppStatus.text = $"Failed to create app! Error code {req.responseCode} (check console)";
                    Debug.LogError(req.error);
                }
            }
        }

        private static void AppSettingsChanged(SerializedPropertyChangeEvent evt)
        {
            SetButtonActive();
        }

        private static void SetButtonActive()
        {
            var canSubmit = CanSubmitNewApp();
            if (!canSubmit && _appDescription.stringValue.Length < 20)
                _createAppStatus.text = "Description must be >= 20 characters long";
            else
                _createAppStatus.text = "";
            _root.Q<Button>("SubmitApp").style.opacity = canSubmit ? 1 : 0.5f;
        }

        private static bool CanSubmitNewApp()
        {
            return !string.IsNullOrWhiteSpace(_appName.stringValue)
                   && !string.IsNullOrWhiteSpace(_appDescription.stringValue)
                   && !string.IsNullOrWhiteSpace(_appUrl.stringValue)
                   && _appDescription.stringValue.Length >= 20;
        }

        private static void ShowApp(HyplayApp app)
        {
            var settings = new SerializedObject(_settings);
            _appList.style.display = DisplayStyle.None;
            _currentApp.style.display = DisplayStyle.Flex;
            //settings.FindProperty("currentApp").boxedValue = app;
            _settings.SetCurrent(app);
            settings.Update();
            settings.ApplyModifiedProperties();
        }

        private static void GetAccessToken(SerializedPropertyChangeEvent evt)
        {
            var newAT = evt.changedProperty.stringValue;
            bool hasAT = !string.IsNullOrWhiteSpace(newAT);
            var display = hasAT ? DisplayStyle.Flex : DisplayStyle.None;

            _appList.style.display = display;
            _root.Q<VisualElement>("AppSettings").style.display = display;
            _root.Q<Button>("GetApps").style.display = display;
            _root.Q<Button>("CreateApp").style.display = display;
        }


        private static void OpenAccountSettings()
        {
            Application.OpenURL("https://hyplay.com/account/settings");
        }
    }
}