using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Harmony;

namespace ModLoaderLibrary
{
    public class ModLoader
    { 
        public static void Entry()
        {
            LogOutput.Initialize();
            LogOutput.Log("Starting ModLoaderLibrary Version " + Constants.verMajor + "." + Constants.verMinor + "." + Constants.verRevision);
            LogOutput.Log("Beginning Patches...");
            try
            {
                var harmony = HarmonyInstance.Create("com.catcherben.modloader");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                LogOutput.Log("Exception when patching: " + e.Message, LogOutput.LogType.ERROR);
                while (e.InnerException != null)
                {
                    LogOutput.Log("Internal Exception Message: " + e.InnerException.Message, LogOutput.LogType.ERROR);
                    e = e.InnerException;
                }
            }
            LogOutput.Log("Finished Patching.");
            LogOutput.Log("Searching for mods...");
            if (!Directory.Exists("mods"))
                Directory.CreateDirectory("mods");
            // For every folder in the mods folder, search for a mod library with the same name
            foreach (string modPath in Directory.GetDirectories("mods"))
            {
                string modName = Path.GetFileName(modPath);
                // Since ModLoader does not support any platforms other than Windows at the moment, we're going to assume the
                // library is a .dll file
                string fullPathToLib = Path.Combine(modPath + "\\" + modName + ".dll");
                if (File.Exists(fullPathToLib))
                {
                    LogOutput.Log("Loading mod " + modName);
                    try
                    {
                        var asm = Assembly.LoadFile(fullPathToLib);
                        // Ensure there is a GameInitializing() function in class Main
                        var init = asm.GetType(modName + ".Main", true).GetMethod("GameInitializing");
                        if (init == null)
                            throw new Exception("Could not find mod initialization function!");

                        // Ensure there is a GameExiting() function in class Main
                        var exit = asm.GetType(modName + ".Main", true).GetMethod("GameExiting");
                        if (exit == null)
                            throw new Exception("Could not find mod exit function!");

                        // Everything's good, add the mod assembly to the list and call the initialize function
                        Constants.modAssemblies.Add(new Tuple<string, Assembly>(modName, asm));
                        init.Invoke(new object(), null);

                        // Increment the mod count number
                        Constants.numMods++;
                    }
                    catch (Exception e)
                    {
                        LogOutput.Log("Mod" + modName + " Failed to Load: " + e.Message, LogOutput.LogType.ERROR);
                    }
                }
            }
        }

        public static void Exit()
        {
            foreach (Tuple<string, Assembly> mod in Constants.modAssemblies)
            {
                // Call each mod's GameExiting() function
                var exit = mod.Item2.GetType(mod.Item1 + ".Main", true).GetMethod("GameExiting");
                exit.Invoke(new object(), null);
            }
            LogOutput.Log("Exiting Airport CEO...");
            LogOutput.DisposeFirst();
        }
    }
}