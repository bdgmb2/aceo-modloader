using System;

namespace ModLoader
{
    class Program
    {
        private static NLog.Logger _logger;

        private static void Main(string[] args)
        {
            _logger = NLog.LogManager.GetCurrentClassLogger();
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            _logger.Info($"Starting ACEO ModLoader version {MLVersion.ToString()}");
            var patcher = new Patcher();

            // RUN LOGIC
            try
            {
                patcher.Run();
            }
            catch (Exception ex)
            {
                _logger.Debug("Attempting to restore backup...");
                patcher.RevertAssembly();
                _logger.Error("ModLoader encountered a problem and has stopped. If you think there is a bug in ModLoader, please submit an issue to the ModLoader Github repository.");
                _logger.Error($"More Information: {ex.Message}");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine($"ModLoader {MLVersion.ToString()}");
            Console.WriteLine("Command line arguments:\r\n");
            Console.WriteLine("-h, --help");
            Console.WriteLine("Prints this help menu.\r\n");
            Console.WriteLine("-ss, --start");
            Console.WriteLine("Start the game through Steam. Note: As of ACEO Alpha 26.1, you cannot start the game any other way.\r\n");
            Console.WriteLine("-p, --patch");
            Console.WriteLine("Patches the game with ModLoaderLibrary\r\n");
            Console.WriteLine("-e, --enums");
            Console.WriteLine("Injects enums found in mods\r\n");
            Console.WriteLine("-b, --backupname");
            Console.WriteLine("Specifies custom name to backup assembly file to\r\n");
            Console.WriteLine("-sp, --steampath");
            Console.WriteLine("Specifies path to Steam.\r\n\r\n\r\n");
            Console.WriteLine("To start ACEO normally from ModLoader, run \"ModLoader [-s or -ss]\"");
            Console.WriteLine("To start ACEO with full modding support, run \"ModLoader -p -e [-s or -ss]\"");
            Console.WriteLine("If you are a mod developer, please refer to the project's Git readme at https://github.com/bdgmb2/aceo-modloader");
            Console.WriteLine("\r\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}