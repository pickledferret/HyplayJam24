using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace HYPLAY.Runtime
{
    // todo: handle cancelled/other errors https://api.wayfarer.games/hyplay-test/redirect.html?error=cancelled
    
    public class HyplaySettings : ScriptableObject
    {
        #if UNITY_EDITOR
        [SerializeField, Space] private string accessToken;
        [SerializeField, Tooltip("The game will automatically use this token in the editor. It is not included in the build.")] private string devToken;
        public string AccessToken => accessToken;
        
        [SerializeField] private string appName;
        [SerializeField] private string appDescription;
        [SerializeField] private string appUrl;
        #endif

        [SerializeField, Tooltip("How many hours after logging in will the token expire?")] 
        private int timeoutHours = 24;
        public int TimeoutHours => timeoutHours;

        [SerializeField, Tooltip("Use a popup window? If disabled, redirects the whole page")]
        private bool usePopup = true;

        [SerializeField] private HyplayApp currentApp;
        public HyplayApp Current => currentApp;
        
        public string Token { get; private set; }

        #if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void DoLoginRedirect(string appid, string expiry);
        [DllImport("__Internal")]
        private static extern void DoLoginPopup(string appid, string expiry);
        #endif
        
        public void SetCurrent(HyplayApp current)
        {
            currentApp = current;
        }

        public void SetToken(string token)
        {
            Token = token;
        }

        internal void DoLogin()
        {
            var time = DateTimeOffset.Now + TimeSpan.FromHours(timeoutHours);
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            if (usePopup)
                DoLoginPopup(Current.id, $"&expiresAt={time.ToUnixTimeSeconds()}");
            else
                DoLoginRedirect(Current.id, $"&expiresAt={time.ToUnixTimeSeconds()}");
            #else
            var redirectUri = Current.redirectUris.First(uri => !uri.Contains("http")) + $"&expiresAt={time.ToUnixTimeSeconds()}";
            #if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(devToken))
                HyplayBridge.DeepLink($"myapp://token#token={devToken}");
            
            redirectUri = Current.redirectUris.First(uri => uri.Contains("http")) + $"&expiresAt={time.ToUnixTimeSeconds()}";
            if (HyplayBridge.IsLoggedIn)
                return;
            #endif
            var url = "https://hyplay.com/oauth/authorize/?appId=" + Current.id + "&chain=HYCHAIN&responseType=token&redirectUri=" + redirectUri;
            Application.OpenURL(url);
            #endif
        }
        
        #if UNITY_EDITOR
        public async Task<List<HyplayApp>> GetApps()
        {
            using var req = UnityWebRequest.Get("https://api.hyplay.com/v1/apps");
            req.SetRequestHeader("x-authorization", accessToken);
            await req.SendWebRequest();
            
            return HyplayJSON.Deserialize<List<HyplayApp>>(req.downloadHandler.text);
        }

        public async void UpdateCurrent()
        {
            #if UNITY_2022_1_OR_NEWER
            using var req = UnityWebRequest.Post($"https://api.hyplay.com/v1/apps/{currentApp.id}", HyplayJSON.Serialize(currentApp), "application/json");
            #else
            using var req = UnityWebRequest.Post($"https://api.hyplay.com/v1/apps/{currentApp.id}", "");
            HyplayJSON.SetData(ref req, HyplayJSON.Serialize(currentApp));
            #endif
            req.method = "PATCH";
            
            req.SetRequestHeader("x-authorization", accessToken);
            await req.SendWebRequest();
            Debug.Log(req.downloadHandler.text);
        }

        public async Task<HyplayImageAsset> CreateAsset(byte[] data)
        {
            var body = new Dictionary<string, string>
            {
                { "fileBase64", System.Convert.ToBase64String(data) }
            };
            
            #if UNITY_2022_1_OR_NEWER
            using var req = UnityWebRequest.Post($"https://api.hyplay.com/v1/assets", HyplayJSON.Serialize(body), "application/json");
            #else
            using var req = UnityWebRequest.Post($"https://api.hyplay.com/v1/assets", "");
            HyplayJSON.SetData(ref req, HyplayJSON.Serialize(body));
            #endif
            req.SetRequestHeader("x-authorization", accessToken);
            await req.SendWebRequest();

            return HyplayJSON.Deserialize<HyplayImageAsset>(req.downloadHandler.text);
        }
        #endif
    }
}