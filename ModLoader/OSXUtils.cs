using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ModLoader
{
    public class OSXUtils
    {
        public static string FindPath(string bundleId)
        {
            var process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName =
                "/System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/LaunchServices.framework/Versions/A/Support/lsregister";
            process.StartInfo.Arguments = "-dump";
            
            process.Start();

            var matcher = new Regex(@"^[^:]+:\s+(?<value>.+)\s\(0x[0-9a-f]+\)$", RegexOptions.Compiled);
            var waitingForPath = false;
            string path = null;

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data == null) return;
                
                var line = args.Data;
                
                if (waitingForPath && line.StartsWith("path:"))
                {
                    var m = matcher.Match(line);
                    if (m.Success)
                    {
                        path = m.Groups["value"].Value;

                        process.Kill();
                        process.CancelOutputRead();
                    }
                }
                else if (line.StartsWith("bundle id:"))
                {
                    var m = matcher.Match(line);
                    if (m.Success && m.Groups["value"].Value == bundleId)
                    {
                        waitingForPath = true;
                    }
                }
            };

            process.BeginOutputReadLine();
            process.WaitForExit();
            process.CancelOutputRead();

            return path;
        }
    }
}