using System;
using System.IO;
using HYPLAY.Runtime;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Hyplay
{
    public class GenerateIFrameOverlay : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.WebGL)
            {
                string indexPath = Path.Combine(report.summary.outputPath, "index.html");

                string htmlContent = File.ReadAllText(indexPath);
                var settings = Resources.Load<HyplaySettings>("Settings");
                var appId = settings.Current.id;
                var time = DateTimeOffset.Now + TimeSpan.FromHours(settings.TimeoutHours);

                // Inject HTML for the overlay
                string overlayHtml = @"
                <div id='fullscreenOverlay' style='
                    display: none; 
                    position: fixed; 
                    top: 0px; 
                    left: 0px; 
                    width: 100%; 
                    height: 100%; 
                    background-color: rgb(0, 0, 0); 
                    z-index: 9999;
                    color: rgb(255, 255, 255);
                    text-align: center;
                '>
                    <div style='height: 100%; display: flex; align-content: center; align-items: center; justify-content: center; flex-direction: column; font-family: sans-serif;'>
                        <div style='margin-top: 15%'>
                            <h1 id='play' style='width: 100%; cursor: pointer;'>Click To Play</h1>
                            <p style='width: 100%; margin-top: 20px;'>or</p>
                            <img id='signin' src='./StreamingAssets/signin.png' style='width: 350px; cursor: pointer;'>
                        </div>
                        <img src='./StreamingAssets/poweredby.png' style='width: 150px; margin-top: 50px;'>
                    </div>
                </div>
            ";

                // Inject JavaScript to control the overlay
                string overlayScript = $@"
                    <script>
                        if (self != top) {{
                            document.getElementById('fullscreenOverlay').style.display = 'block';
                        }}

                        document.getElementById('play').addEventListener('click', function() {{
                            window.top.location = window.location.href;
                        }});

                        document.getElementById('signin').addEventListener('click', function() {{
                            var redirectUri = window.location.href;
                            var url = 'https://hyplay.com/oauth/authorize/?appId=' + '{appId}' + '&chain=HYCHAIN&responseType=token' + '{$"&expiresAt={time.ToUnixTimeSeconds()}"}' + '&redirectUri=' + redirectUri;
                            window.top.location = url;
                        }});
                    </script>
            ";

                htmlContent = htmlContent.Replace("</body>", overlayHtml + overlayScript + "</body>");

                File.WriteAllText(indexPath, htmlContent);
            }
        }
    }
}