using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ModLoader
{
    public abstract class Launcher
    {
        protected readonly NLog.Logger _logger;

        protected Launcher(NLog.Logger logger)
        {
            _logger = logger;
        }
        
        public abstract void Launch();
        
        public void WaitForGameExit()
        {
            Process[] processes = {};
            
            // Wait a few seconds just to be safe
            for (var i = 0; i < 20; i++)
            {
                Thread.Sleep(1000);
                processes = Process.GetProcessesByName("Airport CEO");
                if (processes.Length > 0) break;
            }

            if (processes.Length > 0)
            {
                _logger.Info("Found Airport CEO process. Waiting until exit.");
                _logger.Info("Do NOT close this window! It will close automatically.");
                // We assume the first process that matches Airport CEO is, in fact, the game
                // Wait until the game exits
                SpinWait.SpinUntil(() => processes[0].HasExited);
            }
            else
            {
                _logger.Error("Airport CEO was not launched.");
            }
        }
    }
}
