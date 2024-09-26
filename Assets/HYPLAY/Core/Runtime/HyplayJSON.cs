#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace HYPLAY.Runtime
{
    public static class HyplayJSON
    {
        public static T Deserialize<T>(string json)
        {
            #if NEWTONSOFT_JSON
            return JsonConvert.DeserializeObject<T>(json);
            #endif
            
            #pragma warning disable CS0162
            #if UNITY_EDITOR
            InstallPackage();
            #endif
            Debug.LogWarning("Newtonsoft JSON not installed");
            return default;
        }

        public static string Serialize(object obj)
        {
            #if NEWTONSOFT_JSON
            return JsonConvert.SerializeObject(obj);
            #endif
            
            #pragma warning disable CS0162
            #if UNITY_EDITOR
            InstallPackage();
            #endif
            Debug.LogWarning("Newtonsoft JSON not installed");
            return default;
        }

        public static void SetData(ref UnityWebRequest req, string body)
        {
            req.uploadHandler?.Dispose();
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body))
            {
                contentType = "application/json"
            };
        }
        
        #if UNITY_EDITOR
        public static void InstallPackage()
        {
            #if NEWTONSOFT_JSON
            return;
            #pragma warning disable CS0162
            #endif
            if (UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.ExitPlaymode();
            
            UnityEditor.PackageManager.Client.Add("com.unity.nuget.newtonsoft-json");
        }
        #endif
    }
}