using System;
using System.IO;
using UnityEngine;

namespace DAGP.MCP.Services
{
    /// <summary>Resolves on-disk paths for capture artifacts under &lt;projectRoot&gt;/.dagp/screenshots/.</summary>
    public static class CapturePaths
    {
        public static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        public static string ScreenshotDir
        {
            get
            {
                var dir = Path.Combine(ProjectRoot, ".dagp", "screenshots");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>Build a default timestamped capture path. prefix='gameview' -> gameview_20260430_143022_123.png</summary>
        public static string DefaultPath(string prefix = "capture")
        {
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            return Path.Combine(ScreenshotDir, $"{prefix}_{ts}.png").Replace('\\', '/');
        }

        /// <summary>If user passed a relative path, resolve against the project root.</summary>
        public static string Resolve(string userPath)
        {
            if (string.IsNullOrEmpty(userPath)) return null;
            return Path.IsPathRooted(userPath) ? userPath.Replace('\\', '/')
                                                : Path.Combine(ProjectRoot, userPath).Replace('\\', '/');
        }
    }
}
