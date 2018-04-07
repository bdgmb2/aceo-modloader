using System;
using System.Diagnostics;
using System.Threading;

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

                //patcher.RunMonoCerts();

                patcher.PatchAssembly();

                patcher.ReplaceAssembly();

                Logger.Log("Starting Airport CEO");
                var startGame = new Process();
                // Airport CEO's AppID is 673610
                startGame.StartInfo =
                    new ProcessStartInfo(patcher.SteamPath + "/Steam" + patcher.PlatformExecExtension);
                startGame.StartInfo.WorkingDirectory = patcher.ACEOPath;
                startGame.StartInfo.Arguments = "-applaunch 673610";
                startGame.Start();

                // Steam should be starting up the game now
                startGame.WaitForExit();
                // Wait a second and a half just to be safe
                Thread.Sleep(1500);
                var processes = Process.GetProcessesByName("Airport CEO");
                if (processes.Length > 0)
                {
                    Logger.Log("Found Airport CEO process. Waiting until exit.");
                    Logger.Log("DO NOT CLOSE THIS WINDOW!", Logger.LogType.WARNING);
                    // We're gonna assume the first process that matches Airport CEO is, in fact, the game
                    // Wait until the game exits
                    SpinWait.SpinUntil(() => { return processes[0].HasExited; });
                }

                patcher.RevertState();
            }
            catch (Exception e)
            {
                Logger.Log("Exception Occurred: " + e.Message, Logger.LogType.ERROR);
            }
        }
    }
}