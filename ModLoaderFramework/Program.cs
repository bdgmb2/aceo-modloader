using System;
using System.IO;
using System.Diagnostics;

namespace ModLoader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Logger.SetupLogger("modloader.log");
                Logger.Log("DO NOT CLOSE THIS WINDOW!", Logger.LogType.WARNING);
                var patcher = new Patcher();

                patcher.BackupAssembly();

                patcher.PatchAssembly();

                patcher.ReplaceAssembly();

                Logger.Log("Starting Airport CEO");
                var aceoProcess = new Process();
                aceoProcess.StartInfo =
                    new ProcessStartInfo(patcher.ACEOPath + "/Airport CEO.exe");
                aceoProcess.StartInfo.WorkingDirectory = patcher.ACEOPath;
                aceoProcess.Start();

                aceoProcess.WaitForExit();

                patcher.RevertState();
            }
            catch (Exception e)
            {
                Logger.Log("Exception Occurred: " + e.Message, Logger.LogType.ERROR);
            }
        }
    }
}