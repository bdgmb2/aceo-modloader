using System.Diagnostics;

namespace ModLoader
{
    public class OSXLauncher: Launcher
    {
        private readonly string _appPath;
        private readonly string _gameDirectory;

        public OSXLauncher(NLog.Logger logger, string appPath, string gameDirectory): base(logger)
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
                StartInfo = new ProcessStartInfo("open")
                {
                    WorkingDirectory = _gameDirectory,
                    Arguments = $"-a \"{_appPath}\" --args -applaunch 673610"
                }
            };
            
            process.Start();
            process.WaitForExit();
        }
    }
}