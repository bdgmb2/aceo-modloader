using System.Diagnostics;

namespace ModLoader
{
    public class WindowsLauncher: Launcher
    {
        private readonly string _appPath;
        private readonly string _gameDirectory;

        public WindowsLauncher(NLog.Logger logger, string appPath, string gameDirectory): base(logger)
        {
            _appPath = appPath;
            _gameDirectory = gameDirectory;
        }
        
        public override void Launch()
        {
            _logger.Info("Launching ACEO...");
            _logger.Debug($"Launching ACEO through Steam with {_appPath} -applaunch 673610");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(_appPath)
                {
                    WorkingDirectory = _gameDirectory,
                    Arguments = "-applaunch 673610"
                }
            };
            
            process.Start();
            
            // Steam should be starting up the game now
            process.WaitForExit();
        }
    }
}