using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using HyplayJSON = HYPLAY.Runtime.HyplayJSON;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif
namespace HYPLAY.Leaderboards.Runtime
{
    public class HyplayLeaderboards : ScriptableObject
    {
        [SerializeField] private List<HyplayLeaderboard> leaderboards;
        
        #if UNITY_EDITOR && NEWTONSOFT_JSON
        private const string LeaderboardBasePath = "Assets/HYPLAY/Leaderboards/Resources/leaderboard-";

        public async Task CreateLeaderboard(string appId, string appSecret)
        {
            var body = new Dictionary<string, object>
            {
                { "name", "Leaderboard Name" },
                { "description", "Leaderboard Description" }
            };
            using var req = UnityWebRequest.Post($"https://api.hyplay.com/v1/apps/{appId}/leaderboards",
                HyplayJSON.Serialize(body)
#if UNITY_2022_1_OR_NEWER
                ,"application/json");
#else
                );
                HyplayJSON.SetData(ref req, HyplayJSON.Serialize(body));
#endif
            req.SetRequestHeader("x-app-authorization", appSecret);

            await req.SendWebRequest();
            
            var lb = HyplayJSON.Deserialize<JToken>(req.downloadHandler.text);
            AddLeaderboardObject(appId, appSecret, lb);
            await GetLeaderboards(appId, appSecret);
        }
        
        public async Task GetLeaderboards(string appId, string appSecret)
        {
            if (leaderboards == null) leaderboards = new List<HyplayLeaderboard>();
            using var req = UnityWebRequest.Get($"https://api.hyplay.com/v1/apps/{appId}/leaderboards");
            await req.SendWebRequest();

            var res = HyplayJSON.Deserialize<JArray>(req.downloadHandler.text);
            foreach (var lb in res)
            {
                var idx = leaderboards.FindIndex(check => check.id == lb["id"]?.ToString());
                if (idx == -1)
                    AddLeaderboardObject(appId, appSecret, lb);
                else
                {
                    leaderboards[idx].name = lb["name"]?.ToString();
                    leaderboards[idx].description = lb["description"]?.ToString();
                }
            }
            
            EditorUtility.SetDirty(this);
        }

        private void AddLeaderboardObject(string appId, string appSecret, JToken lb)
        {
            var found = AssetDatabase.LoadAssetAtPath<HyplayLeaderboard>(
                $"{LeaderboardBasePath}{lb["id"]}.asset");
            if (found == null)
            {
                var instance = CreateInstance<HyplayLeaderboard>();
                AssetDatabase.CreateAsset(instance, $"{LeaderboardBasePath}{lb["id"]}.asset");
                found = instance;
            }

            JsonUtility.FromJsonOverwrite(lb.ToString(), found);
            EditorUtility.SetDirty(found); 
            AssetDatabase.SaveAssets();

            if (lb["key"] != null)
                found.secretKey = lb["key"].ToString();
            else
                found.GetSecret(appId, appSecret);
            leaderboards.Add(found);
        }
        #endif
    }
}