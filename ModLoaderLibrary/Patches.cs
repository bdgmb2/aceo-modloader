using System;
using System.Collections;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace ModLoaderLibrary
{
    /// <summary>
    /// When a mod is activated in the Airport CEO window, the LoadMods() function is called. Since ACEO
    /// only supports business and livery mods right now, we can extend this functionality by loading
    /// OTHER files into the game state with the patch to this function. Here is where we will load
    /// code-level mods.
    ///
    /// FOR MODDERS: This function calls your gameLoading() function.
    ///
    /// FOR DEVELOPERS: If you look at GameController.LaunchGame(), you'll notice that all the mod
    /// initializations happen pretty late in the game init... this isn't good for any mods that
    /// want to alter systems that "awake" before mod load, ex. procurement.
    /// </summary>
    [HarmonyPatch(typeof(ModManager))]
    [HarmonyPatch("QueueMods")]
    class QueueModsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(string path)
        {
            Logger.Log($"Queue mod at {path}", Logger.LogType.Debug);
            if (!ModManager.GetModData(path, out ModData data))
            {
                Logger.Log($"Couldn't get modData for {path}!", Logger.LogType.Warning);
                return;
            }

            // We're going to load all ".dll" mods in the folder
            var files = Directory.GetFiles(path, "*.dll");
            if (files.Length == 0)
            {
                Logger.Log("Not code-level mod, skipping");
                return;
            }
            
            foreach (var file in files)
            {
                var dllPath = Path.Combine(path, file);
                var modName = Path.GetFileNameWithoutExtension(file);
                
                Logger.Log($"Attempting to load code-level mod {modName}, file: {dllPath}");

                try
                {
                    var assm = Assembly.LoadFile(dllPath);
                    var mainClass = assm.GetType($"{modName}.Main");
                    if (mainClass == null)
                        throw new Exception(
                            $"Main class not found inside mod namespace. Ensure the \"Main\" class exists and is in the same namespace as {modName}");
                    mainClass.GetMethod("GameLoading")?.Invoke(null, null);

                    //var overridingEnums = assm.GetType("EnumAdditions");
                    //if (overridingEnums != null)
                    //DialogPanel.Instance.ShowMessagePanel($"Note: You must restart Airport CEO to enable {modName}!");
                    GlobalVars.numMods++;
                    GlobalVars.modAssemblies.Add(new Tuple<string, Assembly>(modName, assm));
                }
                catch (Exception e)
                {
                    Logger.Log($"Failed to load mod {data.name}: {e.Message}", Logger.LogType.Error);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(GameController))]
    [HarmonyPatch("Start")]
    class OnGameLoadedPatch
    {
        /// <summary>
        /// After the game has fully loaded, we call each mod's GameLoaded() function, if it has one.
        /// At this time, all the UI and frameworks have loaded, so mods can begin to interact with
        /// systems and the player if they wish.
        /// 
        /// FOR MODDERS: This function calls your GameLoaded() function.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(GameController __instance)
        {
            __instance.StartCoroutine(GameLoaded());
        }

        private static IEnumerator GameLoaded()
        {
            // Wait until the game is fully loaded
            yield return SaveLoadGameDataController.loadComplete;
            foreach (var mod in GlobalVars.modAssemblies)
            {
                Logger.Log($"Attempting to call GameLoaded() for mod {mod.Item1}", Logger.LogType.Debug);
                // Get GameLoaded() function in mod, if it exists and run it
                try
                {
                    mod.Item2.GetType($"{mod.Item1}.Main")?.GetMethod("GameLoaded")?.Invoke(null, null);
                }
                catch (Exception e)
                {
                    Logger.Log($"Code-level mod {mod.Item1} failed to run gameLoaded(): {e.Message}",
                        Logger.LogType.Warning);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ModManager))]
    [HarmonyPatch("ActivateMod")]
    [HarmonyPatch(new Type[] {typeof(string)})]
    class ActivateModPatch
    {
        [HarmonyPostfix]
        static void Postfix(string modID)
        {
            Logger.Log($"Checking mod {modID} for enums...", Logger.LogType.Debug);
            // First, match the mod ID to a mod name. To do that, we have to parse the Mod ID's of all the mods manually.
            var modData = ModManager.GetAllNativeMods();
            foreach (ModData item in modData)
            {
                // found it
                if (item.id == modID)
                {
                    Logger.Log($"Found mod {item.name} that matches mod ID", Logger.LogType.Debug);
                    string modPath =
                        typeof(ModManager).GetMethod("GetModPath", BindingFlags.Static | BindingFlags.NonPublic)
                            ?.Invoke(null, null) as string;
                    string modName = item.name.Replace(" ", "");
                    // Load the mod in reflection only (we're only inspecting it for the Enums class, which requires a game restart)
                    Logger.Log($"Attempting to load lib at {Path.Combine(modPath, modName, (modName + ".dll"))}",
                        Logger.LogType.Debug);
                    try
                    {
                        var asm = Assembly.ReflectionOnlyLoad(Path.Combine(modPath, modName, (modName + ".dll")));
                        if (asm != null)
                            Logger.Log("Success");
                        if (asm?.GetType($"{item.name}.Main.EnumAdditions") != null)
                        {
                            Logger.Log($"Found enums class in {modName}", Logger.LogType.Debug);
                            DialogPanel.Instance.ShowQuestionPanel((answer) =>
                            {
                                if (answer)
                                    MLUtils.RestartGameWithML();
                            }, "This mod requires a restart. Would you like to restart now?", true);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Failed checking {modName} for enums: {e.Message}", Logger.LogType.Warning);
                    }
                    break;
                }
            }
        }
    }
}