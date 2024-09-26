using System;
using UnityEngine;

namespace HYPLAY.Runtime
{
    public class HyplayReceiveMessage : MonoBehaviour
    {
        public Action<string> OnMessageReceived;

        public void ReceiveMessage(string token)
        {
            OnMessageReceived?.Invoke(token);
        }
    }
}