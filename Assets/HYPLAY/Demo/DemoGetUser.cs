using System;
using System.Collections;
using System.Collections.Generic;
using HYPLAY.Leaderboards.Runtime;
using HYPLAY.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HYPLAY.Demo
{
    public class DemoGetUser : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;

        [SerializeField] private HyplayLeaderboard leaderboard;
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);
            if (HyplayBridge.IsLoggedIn)
                text.text = "Logged in!";
            else
                GuestLogin();
            
            if (leaderboard != null)
                GetScores();
        }

        private async void GetScores()
        {
            var scores = await leaderboard.GetScores();
            foreach (var score in scores.Data.scores)
                Debug.Log($"{score.username} got {score.score}");
        }

        private async void GuestLogin()
        {
            var res = await HyplayBridge.GuestLoginAndReturnUserAsync();
            if (res.Success)
                text.text = $"Successfully got guest user {res.Data.Username}";
            else
                text.text = $"Failed to get user: {res.Error}";
        }
        
        public async void Login()
        {
            text.text = "Logging in...";
            await HyplayBridge.LoginAsync();
            text.text = "Logged in!";
        }

        public async void GetUser()
        {
            text.text = "Getting user...";
            var res = await HyplayBridge.GetUserAsync();
            if (res.Success)
                text.text = $"Successfully got user {res.Data.Username}";
            else
                text.text = $"Failed to get user: {res.Error}";
        }

        public async void DeleteSession()
        {
            text.text = "Logging out...";
            await HyplayBridge.LogoutAsync();
            text.text = "Logged out";
        }

        public async void SubmitScore()
        {
            text.text = "Leaderboard is null, please create a leaderboard in the settings, and set it using the dropdown";
            if (leaderboard == null) return;
            text.text = "Submitting score";
            var res = await leaderboard.PostScore(5000.002);
            text.text = string.IsNullOrWhiteSpace(res.Error) ? $"Submitted score of {res.Data.score}" : res.Error;
        }
    }
}