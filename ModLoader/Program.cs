using System;

namespace ModLoader
{
    class Program
    {
        private static void Main(string[] args)
        {
            bool logVerbose = false;
            string logLocation = "", backupName = "", assmOutput = "", steamPath = "";
            bool patching = false, enums = false, launchGame = false;

            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            // PARSE ARGUMENTS
            // This would be a foreach, but I'm doing lookahead to check some arguments
            for (int i = 0; i < args.Length; i++)
            {
                // For simplicity
                string arg = args[i];

                switch (arg)
                {
                    // Turn the verbosity up
                    case "-v":
                    case "--verbose":
                        logVerbose = true;
                        break;

                    // Custom log output location
                    case "-l":
                    case "--log":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("[ERROR] Log is missing file argument. Exiting.");
                            return;
                        }
                        else
                            logLocation = args[i + 1];

                        break;

                    // Patch the assembly
                    case "-p":
                    case "--patch":
                        patching = true;
                        break;

                    // Inject enums
                    case "-e":
                    case "--enums":
                        if (!patching)
                        {
                            Console.WriteLine("Cannot inject enums without patching library. Please run with \"-p\"");
                            return;
                        }
                        else
                            enums = true;

                        break;

                    // Custom assembly backup name
                    case "-b":
                    case "--backupname":
                        if (!patching)
                        {
                            Console.WriteLine(
                                "Cannot specify custom backup name without patching library. Please run with \"-p\"");
                            return;
                        }
                        else if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("[ERROR] Backup file name is missing argument. Exiting.");
                            return;
                        }
                        else
                            backupName = args[i + 1];

                        break;

                    // Custom patched assembly output name
                    case "-o":
                    case "--output":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("[ERROR] Library output is missing file argument. Exiting.");
                            return;
                        }
                        else
                            assmOutput = args[i + 1];

                        break;

                    // Launch game through Steam
                    case "-s":
                    case "--start":
                        launchGame = true;
                        break;

                    // Specify Steam Path
                    case "-sp":
                    case "--steampath":
                        if (i + 1 >= args.Length)
                        {
                            Console.WriteLine("[ERROR] Steam Path is missing argument. Exiting.");
                            return;
                        }
                        else
                            steamPath = args[i + 1];

                        break;

                    // Show help menu
                    case "-h":
                    case "--help":
                    default:
                        PrintHelp();
                        return;
                }
            }

            // INITIATE LOGGER
            if (logLocation != "")
                Logger.Initialize(logVerbose, logLocation);
            else
                Logger.Initialize(logVerbose);

            Logger.Log($"Starting ACEO ModLoader version {MLVersion.ToString()}");

            // INITIATE PATCHER
            Patcher patcher;
            try
            {
                patcher = new Patcher
                {
                    IsPatching = patching,
                    IsInjectingEnums = enums,
                    IsLaunchingGame = launchGame
                };
                if (backupName != "")
                    patcher.BackupFilename = backupName;
                if (assmOutput != "")
                    patcher.AssemblyName = assmOutput;
                if (steamPath != "")
                    patcher.SteamDirectory = steamPath;
            }
            catch (Exception ex)
            {
                Logger.Log($"Critical error when starting up: {ex.Message}", Logger.LogType.Error);
                Logger.Log("Press any key to continue.");
                Console.ReadKey();
                return;
            }

            // RUN LOGIC
            try
            {
                patcher.Run();
            }
            catch (Exception)
            {
                Logger.Log("Attempting to restore backup...");
                patcher.RevertAssembly();
                Logger.Log("ModLoader encountered a problem and has stopped. If you think there is a bug in ModLoader, please submit an issue to the ModLoader Github repository.", Logger.LogType.Error);
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
            }

            Logger.Log("ModLoader Exiting...");
        }

        private static void PrintHelp()
        {
            Console.WriteLine($"ModLoader {MLVersion.ToString()}");
            Console.WriteLine("Command line arguments:\r\n");
            Console.WriteLine("-h, --help");
            Console.WriteLine("Prints this help menu.\r\n");
            Console.WriteLine("-v, --verbose");
            Console.WriteLine("Enables verbose debug output to log and console.\r\n");
            Console.WriteLine("-ss, --start");
            Console.WriteLine("Start the game through Steam. Note: As of Alpha 26.1, you cannot start the game any other way.\r\n");
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