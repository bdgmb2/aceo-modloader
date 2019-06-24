using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace ModLoader
{
    internal class Patcher
    {
        // Internal variables
        private readonly string _gameDirectory;
        private readonly string _assemblyDirectory;
        private readonly string _execExtension;
        private readonly string _libExtension;
        private ModuleDefinition module;

        private readonly NLog.Logger _logger;

        public Patcher()
        {
            _logger = NLog.LogManager.GetCurrentClassLogger();
            _execExtension = ConfigManager.CurrentPlatform == OSType.Windows ? ".exe" : ".app";
            _libExtension = ConfigManager.CurrentPlatform == OSType.Windows ? ".dll" : ".dylib";
            _gameDirectory = FindACEODirectory(ConfigManager.SteamDirectory);
            _assemblyDirectory = Path.Combine(_gameDirectory, "Airport CEO_Data", "Managed");
        }

        private static string FindACEODirectory(string steamDirectory)
        {
            return ConfigManager.CurrentPlatform == OSType.Windows ? 
                Path.Combine(steamDirectory, "steamapps", "common", "Airport CEO") :
                "~/Library/Application Support/Steam/steamapps/common/Airport CEO";
        }

        public void Run()
        {
            _logger.Debug($"Steam Directory is {ConfigManager.SteamDirectory}");
            _logger.Debug($"ACEO Directory is {_gameDirectory}");
            _logger.Debug($"Assembly-CSharp Path is {Path.Combine(_assemblyDirectory, ConfigManager.AssemblyName)}");

            if (ConfigManager.IsPatching)
            {
                BackupAssembly();
                // Add library resolution (ACEO now relies on other DLL's like DOTween)
                var libResolver = new DefaultAssemblyResolver();
                libResolver.AddSearchDirectory(_assemblyDirectory);
                // Create Module
                module = AssemblyDefinition
                    .ReadAssembly(Path.Combine(_assemblyDirectory, ConfigManager.BackupFilename), new ReaderParameters { AssemblyResolver = libResolver})
                    .MainModule;
                Patch();

                if (ConfigManager.IsInjectingEnums)
                {
                    InjectEnums();
                }

                // Write out to file
                try
                {
                    module.Write(Path.Combine(_assemblyDirectory, ConfigManager.AssemblyName));
                    // Calling this manually to free the file lock
                    module.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Error("Unable to write patched assembly! Are you sure it's not open somewhere else?");
                    _logger.Error($"Additional information: {ex.Message}");
                    throw;
                }
            }
            if (ConfigManager.IsLaunchingGame)
            {
                CheckForMLL();
                _logger.Info("Launching ACEO...");
                _logger.Debug($"Launching ACEO through Steam with {Path.Combine(ConfigManager.SteamDirectory, $"Steam{_execExtension}")} -applaunch 673610");
                // Start ACEO through Steam
                var startGame = new Process
                {
                    StartInfo = new ProcessStartInfo(Path.Combine(ConfigManager.SteamDirectory, $"Steam{_execExtension}"))
                    {
                        WorkingDirectory = _gameDirectory,
                        Arguments = "-applaunch 673610"
                    }
                };
                startGame.Start();

                // Steam should be starting up the game now
                startGame.WaitForExit();

                // Wait a second and a half just to be safe
                Thread.Sleep(1500);
                var processes = Process.GetProcessesByName("Airport CEO");
                if (processes.Length > 0)
                {
                    _logger.Info("Found Airport CEO process. Waiting until exit.");
                    _logger.Info("Do NOT close this window! It will close automatically.");
                    // We assume the first process that matches Airport CEO is, in fact, the game
                    // Wait until the game exits
                    SpinWait.SpinUntil(() => processes[0].HasExited);
                }

                if (ConfigManager.IsPatching)
                {
                    RevertAssembly();
                }
            }
        }

        private void BackupAssembly()
        {
            _logger.Debug($"Backing up ACEO Assembly to {ConfigManager.BackupFilename}");
            // If the backup filename already exists, we don't want to overwrite it.
            if (File.Exists(Path.Combine(_assemblyDirectory, ConfigManager.BackupFilename)))
            {
                _logger.Info("Backup assembly already exists! Reverting backup to main first.");
            }
            try
            {
                File.Copy(Path.Combine(_assemblyDirectory, ConfigManager.AssemblyName), Path.Combine(_assemblyDirectory, ConfigManager.BackupFilename), true);
            }
            catch (Exception ex)
            {
                _logger.Error($"Backing up Assembly Failed, Not Safe to Continue: {ex.Message}");
                throw;
            }
            _logger.Debug("Done backing up.");
        }

        private void CheckForMLL()
        {
            try
            {
                if (Directory.Exists(Path.Combine(_gameDirectory, "ModLoader")))
                {
                    _logger.Debug("Clearing ModLoader directory...");
                    Directory.Delete(Path.Combine(_gameDirectory, "ModLoader"), true);
                }
                SpinWait.SpinUntil(() => !Directory.Exists(Path.Combine(_gameDirectory, "ModLoader")));
                _logger.Info("Writing MLL...");
                if (!File.Exists($"MLL{_libExtension}") || !File.Exists($"0Harmony{_libExtension}"))
                {
                    throw new Exception("MLL and dependencies could not be found. ModLoader installation is missing files. Cannot continue.");
                }
                Directory.CreateDirectory(Path.Combine(_gameDirectory, "ModLoader"));
                SpinWait.SpinUntil(() => Directory.Exists(Path.Combine(_gameDirectory, "ModLoader")));
                File.Copy($"MLL{_libExtension}", Path.Combine(_gameDirectory, "ModLoader", $"MLL{_libExtension}"));
                File.Copy($"0Harmony{_libExtension}", Path.Combine(_gameDirectory, "ModLoader", $"0Harmony{_libExtension}"));
            }
            catch (Exception ex)
            {
                _logger.Error($"Error when adding MLL to ACEO Directory: {ex.Message}");
                throw;
            }
        }

        private void Patch()
        {
            try
            {
                _logger.Debug($"Patching Assembly-CSharp{_libExtension}");
                
                // This is the ExitPoint class we're patching
                var exitPointIL = module.Types.Single(x => x.Name == "Utils").Methods.Single(x => x.Name == "QuitGame").Body;
                var exitPointProcessor = exitPointIL.GetILProcessor();
                
                // Remove all instructions (It's only a call to Application.Quit() and a 'ret')
                exitPointIL.Instructions.Clear();
                
                // Load the ModLoaderLibrary assembly
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Ldstr, $"ModLoader/MLL{_libExtension}"));
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

                var reference = typeof(UnityEngine.Application).GetMethod("Quit", BindingFlags.Static | BindingFlags.Public | BindingFlags.OptionalParamBinding, null, new Type[] { }, null);

                // Add the 'Application.Quit()' back in
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Call, module.ImportReference(reference)));
                // Add the 'ret' back in
                exitPointIL.Instructions.Add(exitPointProcessor.Create(OpCodes.Ret));
                // Ok we're done with the exitpoint.

                // This is the entry point method we're patching
                var entryPointIL = module.Types.Single(x => x.Name == "ApplicationVersionLabelUI").Methods.Single(x => x.Name == "Awake").Body;
                var entryPointProcessor = entryPointIL.GetILProcessor();

                // Remove the 'ret' at the end of the instruction list in the method
                entryPointIL.Instructions.RemoveAt(entryPointIL.Instructions.Count - 1);

                // Add ModLoader Message (since we're at the end of GameVersionLabelUI)
                var gameVerReference = module.Types.Single(x => x.Name == "ApplicationVersionLabelUI");
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldarg_0));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldfld, module.ImportReference(gameVerReference.Fields.Single(x => x.Name == "versionLabelText"))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Dup));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(UnityEngine.UI.Text).GetMethod("get_text"))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldstr, " - ModLoader " + MLVersion.ToString()));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Call, module.ImportReference(typeof(System.String).GetMethod("Concat", new [] { typeof(string), typeof(string) }))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(UnityEngine.UI.Text).GetMethod("set_text", new [] { typeof(string) }))));

                // Load Harmony
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldstr, $"ModLoader/0Harmony{_libExtension}"));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Call, module.ImportReference(typeof(Assembly).GetMethod("LoadFile", new[] { typeof(string) }))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Pop));

                // Load the ModLoaderLibrary assembly
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldstr, $"ModLoader/MLL{_libExtension}"));
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
                if (_logger.IsDebugEnabled)
                {
                    entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldc_I4_1));
                }
                else
                {
                    entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ldc_I4_0));
                }
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Box, module.ImportReference(typeof(Boolean))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Stelem_Ref));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Callvirt, module.ImportReference(typeof(MethodBase).GetMethod("Invoke", new[] { typeof(object), typeof(object[]) }))));
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Pop));

                // Add the 'ret' back in
                entryPointIL.Instructions.Add(entryPointProcessor.Create(OpCodes.Ret));

                _logger.Debug("Done.");
            }
            catch (Exception)
            {
                module.Dispose();
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
                _logger.Debug($"Checking mod \"{modName}\" for enums");

                // Check mod for enums class
                if (File.Exists(Path.Combine(modDir, $"{modName}{_libExtension}")))
                {
                    var modAdd = AssemblyDefinition.ReadAssembly(Path.Combine(modDir, $"{modName}{_libExtension}")).MainModule;
                    // Get the "EnumAdditions" class
                    var enumadditions = modAdd?.GetType($"{modName}.EnumAdditions");
                    if (enumadditions == null || !enumadditions.IsClass)
                        continue;

                    foreach (var enumAdd in enumadditions.NestedTypes)
                    {
                        if (enumAdd.IsEnum)
                        {
                            _logger.Debug($"Adding mod enum \"{enumAdd.Name}\" values to ACEO enum.");
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
                                _logger.Debug("Done.");
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn($"Exception when adding enum {enumAdd.Name}: {ex.Message}");
                                _logger.Warn($"This mod ({modName}) may not work correctly, or even crash ACEO!");
                            }
                        }
                    }
                }
            }
        }

        public void RevertAssembly()
        {
            _logger.Debug("Replacing patched assembly with original...");
            try
            {
                File.Delete(Path.Combine(_assemblyDirectory, ConfigManager.AssemblyName));
                File.Move(Path.Combine(_assemblyDirectory, ConfigManager.BackupFilename), Path.Combine(_assemblyDirectory, ConfigManager.AssemblyName));
            }
            catch (Exception e)
            {
                _logger.Error($"Reverting Assembly Failed: {e.Message}");
            }
            _logger.Debug("Done.");
        }
    }
}