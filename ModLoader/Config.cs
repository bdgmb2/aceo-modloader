using System;
using System.IO;
using System.Configuration;
using Microsoft.Win32;

namespace ModLoader
{
    public enum OSType
    {
        Windows,
        OSX
    }

    public static class ConfigManager
    {
        public static bool IsPatching { get; private set; }
        public static bool IsInjectingEnums { get; private set; }
        public static bool IsLaunchingGame { get; private set; }
        public static string SteamDirectory { get; private set; }
        public static string AssemblyName { get; private set; }
        public static string BackupFilename { get; private set; }

        public static OSType CurrentPlatform { get; private set; }

        static ConfigManager()
        {
            int platform = (int)Environment.OSVersion.Platform;
            if (platform == 4 || platform == 6 || platform == 128) // On OSX
            {
                CurrentPlatform = OSType.OSX;
            }
            else
            {
                CurrentPlatform = OSType.Windows;
            }
                
            IsInjectingEnums = ConfigurationManager.AppSettings["inject_enums"] == "false" ? false : true;
            IsPatching = ConfigurationManager.AppSettings["patch"] == "false" ? false : true;
            IsLaunchingGame = ConfigurationManager.AppSettings["launch_game"] == "false" ? false : true;
            SteamDirectory = ConfigurationManager.AppSettings["steam_directory"] ?? FindSteamDirectory();
            AssemblyName = ConfigurationManager.AppSettings["assembly_name"] ?? $"Assembly-CSharp.{(CurrentPlatform == OSType.Windows ? "dll" : "dylib")}";
            BackupFilename = ConfigurationManager.AppSettings["backup_filename"] ?? $"{AssemblyName}.BACKUP";
        }

        private static string FindSteamDirectory()
        {
            var _logger = NLog.LogManager.GetCurrentClassLogger();
            if (CurrentPlatform == OSType.OSX)
            {
                if (!File.Exists("~/Applications/Steam.app"))
                {
                    _logger.Fatal("Steam installation not found at \"~/Applications/Steam.app\". You may need to manually specify the path to Steam.");
                    _logger.Fatal("ModLoader cannot continue. Press any key to exit.");
                    Console.ReadKey();
                    Environment.Exit(3);
                }
                return "~/Applications/Steam.app";
            }
            else
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if (key.GetValue("SteamPath") == null)
                    {
                        _logger.Fatal("Steam installation not found in Windows registry. Is Steam installed correctly? You can optionally specify the path to Steam with \"-sp\"");
                        _logger.Fatal("ModLoader cannot continue. Press any key to exit.");
                        Console.ReadKey();
                        Environment.Exit(3);
                    }
                    return key.GetValue("SteamPath").ToString();
                }
            }
        }
    }
}
