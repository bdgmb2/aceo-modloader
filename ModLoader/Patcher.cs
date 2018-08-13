using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace ModLoader
{
    internal class Patcher
    {
        // Accessed variables from Main()
        public bool IsPatching { get; set; }
        public bool IsLaunchingGame { get; set; }
        public string SteamDirectory { get; set; }
        public bool IsInjectingEnums { get; set; }
        public string AssemblyName { get; set; }
        public string BackupFilename { get; set; }

        public string LibExtension { get; }
        public string ExecExtenstion { get; }

        // Internal variables
        private string _gameDirectory;
        private string _assemblyDirectory;
        private enum PlatformType { WIN, OSX };
        private readonly PlatformType _platform;
        private ModuleDefinition module;

        public Patcher()
        {
            int platform = (int) Environment.OSVersion.Platform;
            if (platform == 4 || platform == 6 || platform == 128) // On OSX
            {
                LibExtension = ".dylib";
                ExecExtenstion = ".app";
                _platform = PlatformType.OSX;
            }
            else // On Windows
            {
                LibExtension = ".dll";
                ExecExtenstion = ".exe";
                _platform = PlatformType.WIN;
            }

            IsPatching = false;
            IsInjectingEnums = false;
            IsLaunchingGame = false;
            BackupFilename = "Assembly-CSharpBackup" + LibExtension;
            AssemblyName = "Assembly-CSharp" + LibExtension;
        }

        private string FindACEODirectory(string steamDirectory)
        {
            return _platform == PlatformType.WIN ? 
                Path.Combine(steamDirectory, "steamapps", "common", "Airport CEO") :
                "~/Library/Application Support/Steam/steamapps/common/Airport CEO";
        }

        private string FindSteamDirectory()
        {
            if (_platform == PlatformType.OSX)
            {
                if (!File.Exists("~/Applications/Steam.app"))
                    throw new Exception("Steam installation not found at \"~/Applications/Steam.app\". You may need to manually specify the path to Steam.");
                return "~/Applications/Steam.app";
            }
            else
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if (key.GetValue("SteamPath") == null)
                        throw new Exception("Steam installation not found in Windows registry. Is Steam installed correctly? You can optionally specify the path to Steam with \"-sp\"");
                    else
                        return key.GetValue("SteamPath").ToString();
                }
            }
        }

        public void Run()
        {
            // Verify all paths
            if (string.IsNullOrEmpty(SteamDirectory))
                SteamDirectory = FindSteamDirectory();
            _gameDirectory = FindACEODirectory(SteamDirectory);
            _assemblyDirectory = Path.Combine(_gameDirectory, "Airport CEO_Data", "Managed");

            Logger.Log("Steam Directory is " + SteamDirectory, Logger.LogType.Debug);
            Logger.Log("ACEO Directory is " + _gameDirectory, Logger.LogType.Debug);
            Logger.Log("Assembly-CSharp Path is " + Path.Combine(_assemblyDirectory, AssemblyName), Logger.LogType.Debug);

            // Check if the "mods" folder exists
            if (!Directory.Exists(Path.Combine(_gameDirectory, "mods")))
            {
                Logger.Log("Mods folder does not exist. Creating...");
                try
                {
                    Directory.CreateDirectory(Path.Combine(_gameDirectory, "mods"));
                }
                catch (Exception e)
                {
                    Logger.Log("Creating mods directory failed: " + e.Message, Logger.LogType.Warning);
                }
            }

            if (IsPatching)
            {
                BackupAssembly();
                // Add library resolution (ACEO now relies on other DLL's like DOTween)
                var libResolver = new DefaultAssemblyResolver();
                libResolver.AddSearchDirectory(_assemblyDirectory);
                // Create Module
                module = AssemblyDefinition
                    .ReadAssembly(Path.Combine(_assemblyDirectory, BackupFilename), new ReaderParameters { AssemblyResolver = libResolver})
                    .MainModule;
                Patch();

                if (IsInjectingEnums)
                {
                    InjectEnums();
                }

                // Write out to file
                try
                {
                    module.Write(Path.Combine(_assemblyDirectory, AssemblyName));
                    // Calling this manually to free the file lock
                    module.Dispose();
                }
                catch (Exception)
                {
                    Logger.Log("Unable to write patched assembly! Are you sure it's not open somewhere else?", Logger.LogType.Error);
                    throw;
                }
            }
            if (IsLaunchingGame)
            {
                CheckForMLL();
                Logger.Log("Launching ACEO...");
                Logger.Log($"Launching ACEO through Steam with {Path.Combine(SteamDirectory, "Steam" + ExecExtenstion)} -applaunch 673610", Logger.LogType.Debug);
                // Start ACEO through Steam
                var startGame = new Process();
                startGame.StartInfo =
                    new ProcessStartInfo(Path.Combine(SteamDirectory, "Steam" + ExecExtenstion));
                startGame.StartInfo.WorkingDirectory = _gameDirectory;
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
                    Logger.Log("Do NOT close this window! It will close automatically.");
                    // We assume the first process that matches Airport CEO is, in fact, the game
                    // Wait until the game exits
                    SpinWait.SpinUntil(() => processes[0].HasExited);
                }

                if (IsPatching)
                    RevertAssembly();
            }
        }

        private void BackupAssembly()
        {
            Logger.Log("Backing up ACEO Assembly to " + BackupFilename, Logger.LogType.Debug);
            // If the backup filename already exists, we don't want to overwrite it.
            if (File.Exists(Path.Combine(_assemblyDirectory, BackupFilename)))
                Logger.Log("Backup file already exists! Reverting backup to main first.", Logger.LogType.Warning);

            try
            {
                File.Copy(Path.Combine(_assemblyDirectory, AssemblyName), Path.Combine(_assemblyDirectory, BackupFilename), true);
            }
            catch (Exception e)
            {
                Logger.Log("Backing up Assembly Failed: " + e.Message, Logger.LogType.Error);
                throw;
            }
            Logger.Log("Done.", Logger.LogType.Debug);
        }

        private void CheckForMLL()
        {
            try
            {
                if (Directory.Exists(Path.Combine(_gameDirectory, "ModLoader")))
                    Directory.Delete(Path.Combine(_gameDirectory, "ModLoader"), true);
                SpinWait.SpinUntil(() => !Directory.Exists(Path.Combine(_gameDirectory, "ModLoader")));
                Logger.Log("Writing MLL...");
                if (!File.Exists("MLL" + LibExtension) || !File.Exists("0Harmony" + LibExtension))
                    throw new Exception(
                        "MLL and dependencies could not be found. ModLoader installation is missing files. Cannot continue.");
                try
                {
                    Directory.CreateDirectory(Path.Combine(_gameDirectory, "ModLoader"));
                }
                catch (Exception) { }
                SpinWait.SpinUntil(() => Directory.Exists(Path.Combine(_gameDirectory, "ModLoader")));
                File.Copy("MLL" + LibExtension, Path.Combine(_gameDirectory, "ModLoader", "MLL" + LibExtension));
                File.Copy("0Harmony" + LibExtension,
                    Path.Combine(_gameDirectory, "ModLoader", "0Harmony" + LibExtension));
            }
            catch (Exception ex)
            {
                Logger.Log($"Error when adding MLL to ACEO Directory, {ex.Message}", Logger.LogType.Error);
                throw;
            }
        }

        private void Patch()
        {
            try
            {
                Logger.Log("Patching Assembly-CSharp" + LibExtension, Logger.LogType.Debug);
                
                // This is the ExitPoint class we're patching
                var exitPointIL = module.Types.Single(x => x.Name == "Utils").Methods.Single(x => x.Name == "QuitGame").Body;
                var exitPointProcessor = exitPointIL.GetILProcessor();
                
                // Remove all instructions (It's only a call to Application.Quit() and a 'ret')
                exitPointIL.Instructions.Clear();
                
                // Load the ModLoaderLibrary assembly
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Ldstr, "ModLoader/MLL" + LibExtension));
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Call, module.ImportReference(typeof(Assembly).GetMethod("LoadFile", new[] { typeof(string) }))));
                // Get the ModLoader class in the assembly
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Ldstr, "ModLoaderLibrary.ModLoader"));
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(Assembly).GetMethod("GetType", new[] { typeof(string) }))));
                // Get the Exit() function in the ModLoader class
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Ldstr, "Exit"));
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(Type).GetMethod("GetMethod", new[] { typeof(string) }))));
                // Call the Exit() function
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Ldnull));
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Ldnull));
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(MethodBase).GetMethod("Invoke", new[] { typeof(object), typeof(object[]) }))));
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Pop));

                // Add the 'Application.Quit()' back in
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Call, module.ImportReference(typeof(UnityEngine.Application).GetMethod("Quit"))));
                // Add the 'ret' back in
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Ret));
                // Ok we're done with the exitpoint.

                // This is the entry point method we're patching
                var entryPointIL = module.Types.Single(x => x.Name == "GameVersionLabelUI").Methods.Single(x => x.Name == "Awake").Body;
                var entryPointProcessor = entryPointIL.GetILProcessor();

                // Remove the 'ret' at the end of the instruction list in the method
                entryPointIL.Instructions.RemoveAt(entryPointIL.Instructions.Count - 1);

                // Add ModLoader Message (since we're at the end of GameVersionLabelUI)
                var gameVerReference = module.Types.Single(x => x.Name == "GameVersionLabelUI");
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldarg_0));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldfld, module.ImportReference(gameVerReference.Fields.Single(x => x.Name == "versionLabelText"))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Dup));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(UnityEngine.UI.Text).GetMethod("get_text"))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldstr, " - ModLoader " + MLVersion.ToString()));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Call, module.ImportReference(typeof(System.String).GetMethod("Concat", new [] { typeof(string), typeof(string) }))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(UnityEngine.UI.Text).GetMethod("set_text", new [] { typeof(string) }))));

                // Load Harmony
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldstr, "ModLoader/0Harmony" + LibExtension));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Call, module.ImportReference(typeof(Assembly).GetMethod("LoadFile", new[] { typeof(string) }))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Pop));

                // Load the ModLoaderLibrary assembly
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldstr, "ModLoader/MLL" + LibExtension));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Call, module.ImportReference(typeof(Assembly).GetMethod("LoadFile", new[] { typeof(string) }))));

                // Get the ModLoader class in the assembly
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldstr, "ModLoaderLibrary.ModLoader"));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(Assembly).GetMethod("GetType", new[] { typeof(string) }))));
                // Get the Entry() function in the ModLoader class
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldstr, "Entry"));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(Type).GetMethod("GetMethod", new[] { typeof(string) }))));
                // Call the Entry() function
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldnull));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldc_I4_1));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Newarr, module.ImportReference(typeof(Object))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Dup));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldc_I4_0));
                if (Logger.logDebug)
                    entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldc_I4_1));
                else
                    entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldc_I4_0));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Box, module.ImportReference(typeof(Boolean))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Stelem_Ref));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(MethodBase).GetMethod("Invoke", new[] { typeof(object), typeof(object[]) }))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Pop));

                // Add the 'ret' back in
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ret));

                Logger.Log("Done.", Logger.LogType.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log("Problem when patching assembly: " + ex.Message + ", Reverting.", Logger.LogType.Error);
                module.Dispose();
                RevertAssembly();
                throw;
            }
        }

        private void InjectEnums()
        {
            string modPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Apoapsis Studios", "Airport CEO", "Mods");
            var dirs = Directory.EnumerateDirectories(modPath);
            foreach (var modDir in dirs)
            {
                // Get name of mod
                string modName = Path.GetFileName(modDir);
                Logger.Log("Checking mod \"" + modName + "\" for enums class", Logger.LogType.Debug);

                // Check mod for enums class
                if (File.Exists(Path.Combine(modDir, modName + LibExtension)))
                {
                    var modAdd = AssemblyDefinition.ReadAssembly(Path.Combine(modDir, modName + LibExtension)).MainModule;
                    // Get the "EnumAdditions" class
                    var enumadditions = modAdd?.GetType(modName + ".EnumAdditions");
                    if (enumadditions == null || !enumadditions.IsClass)
                        continue;

                    foreach (var enumAdd in enumadditions.NestedTypes)
                    {
                        if (enumAdd.IsEnum)
                        {
                            Logger.Log($"Adding mod enum \"{enumAdd.Name}\" values to ACEO enum.", Logger.LogType.Debug);
                            try
                            {
                                var enumToOverride = module.Types.Single(x => x.Name == "Enums").NestedTypes
                                    .Single(x => x.Name == enumAdd.Name && x.IsEnum);
                                var shift = enumToOverride.Fields.Count - 1;
                                foreach (var enumFieldAdd in enumAdd.Fields)
                                {
                                    if (enumFieldAdd.Name != "value__")
                                    {
                                        // For each enum to add on the mod side,
                                        // Create a new enum field on the ACEO side
                                        // Set properties of enum to match enum spec with correct offset
                                        var clonedEnumValue = new FieldDefinition(enumFieldAdd.Name, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, enumToOverride);
                                        clonedEnumValue.Constant = shift++;
                                        enumToOverride.Fields.Add(clonedEnumValue);
                                    }
                                }
                                Logger.Log("Done.", Logger.LogType.Debug);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log($"Exception when adding enum {enumAdd.Name}: {ex.Message}", Logger.LogType.Warning);
                            }
                        }
                    }
                }
            }
        }

        public void RevertAssembly()
        {
            Logger.Log("Replacing patched assembly with original...", Logger.LogType.Debug);
            try
            {
                File.Delete(Path.Combine(_assemblyDirectory, AssemblyName));
                File.Move(Path.Combine(_assemblyDirectory, BackupFilename), Path.Combine(_assemblyDirectory, AssemblyName));
            }
            catch (Exception e)
            {
                Logger.Log("Reverting Assembly Failed: " + e.Message, Logger.LogType.Error);
            }
            Logger.Log("Done.", Logger.LogType.Debug);
        }
    }
}