using System;
using System.Threading.Tasks;
#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace HYPLAY.Runtime
{
    [Serializable]
    public class HyplayAppState<T>
    {
        #if NEWTONSOFT_JSON
        [JsonProperty("key")]
        #endif
        public string Key;
        #if NEWTONSOFT_JSON
        [JsonProperty("overwrite")]
        #endif 
        public bool Overwrite;
        #if NEWTONSOFT_JSON
        [JsonProperty("publicState")]
        #endif 
        public T PublicState;
        #if NEWTONSOFT_JSON
        [JsonProperty("protectedState")]
        #endif 
        public T ProtectedState;
        #if NEWTONSOFT_JSON
        [JsonProperty("privateState")]
        #endif 
        public T PrivateState;

        public async Task<HyplayResponse<HyplayAppState<T>>> SetState()
        {
            return await HyplayBridge.SetState(this);
        }
        
        public static HyplayAppState<T> WithPublicState(string key, bool overwrite, T publicState)
        {
            return new HyplayAppState<T>
            {
                Key = key,
                Overwrite = overwrite,
                PublicState = publicState
            };
        }
        
        public static HyplayAppState<T> WithProtectedState(string key, bool overwrite, T protectedState)
        {
            return new HyplayAppState<T>
            {
                Key = key,
                Overwrite = overwrite,
                ProtectedState = protectedState
            };
        }
        
        public static HyplayAppState<T> WithPrivateState(string key, bool overwrite, T privateState)
        {
            return new HyplayAppState<T>
            {
                Key = key,
                Overwrite = overwrite,
                PrivateState = privateState
            };
        }
    }
}