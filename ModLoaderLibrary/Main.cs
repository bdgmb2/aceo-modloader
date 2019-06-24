using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using HarmonyLib;

namespace ModLoaderLibrary
{
    public class ModLoader
    { 
        public static void Entry(bool verbosity)
        {
            Logger.Initialize(verbosity);
            Logger.Log($"Starting ModLoaderLibrary Version {MLVersion.ToString()}");
            Logger.Log("Beginning Patches...", Logger.LogType.Debug);
            try
            {
                // Yup, this is still necessary
                var harmony = new Harmony("com.catcherben.modloader");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Logger.Log("Finished MLL Patching.", Logger.LogType.Debug);
            }
            catch (Exception e)
            {
                Logger.Log($"Exception when patching: {e.Message}", Logger.LogType.Error);
                while (e.InnerException != null)
                {
                    Logger.Log($"Internal Exception Message: {e.InnerException.Message}", Logger.LogType.Error);
                    e = e.InnerException;
                }
            }

            // All mod-loading code now occurs in a harmony patch, now that ACEO supports modding files, etc. 
            Logger.Log("ModLoaderLibrary initialization finished.");
        }

        public static void Exit()
        {
            foreach (var mod in GlobalVars.modAssemblies)
            {
                // Call each mod's GameExiting() function
                var exit = mod.Item2.GetType(mod.Item1 + ".Main", true).GetMethod("GameExiting");
                if (exit != null)
                    exit.Invoke(new object(), null);
                else Logger.Log($"Mod {mod.Item1} does not have exit function. Skipping.", Logger.LogType.Debug);
            }
            Logger.Log("Exiting Airport CEO...");
            Logger.DisposeFirst();
        }
    }
}