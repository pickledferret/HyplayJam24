using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

namespace UnityEditor.Hyplay
{
    public class HyplayAddRedirectOnBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }
        public void OnPostprocessBuild(BuildReport report)
        {
            #if !UNITY_WEBGL
            return;
            #endif
            // 1. Get the Build Output Path
            var buildOutputPath = report.summary.outputPath;
            const string filePath = "redirect.html";
            var targetFilePath = Path.Combine(buildOutputPath, filePath); 

            File.WriteAllText(targetFilePath, "");
        }
    }
}