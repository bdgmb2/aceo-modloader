using System;
using System.Collections.Generic;
using Microsoft.Win32;
using Mono.Cecil;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;

namespace ModLoader
{
    public class Patcher
    {
        public enum Platforms { OSX, Windows };
        public Platforms CurrentPlatform { get; }
        public string PlatformExecExtension { get; }
        public string PlatformLibExtension { get; }
        public string SteamPath { get; }
        public string ACEOPath { get; }
        public string ACEOExec { get; }
        private readonly string AssemblyPath;
        private readonly string ModLoaderLibPath;

        public Patcher()
        {
            // Get Operating System
            // Code from http://mono.wikia.com/wiki/Detecting_the_execution_platform
            int platformID = (int)Environment.OSVersion.Platform;
            if (platformID == 4 || platformID == 6 || platformID == 128)
            {
                CurrentPlatform = Platforms.OSX;
                PlatformExecExtension = ".app";
                PlatformLibExtension = ".dylib";
                
            }
            else
            {
                CurrentPlatform = Platforms.Windows;
                PlatformExecExtension = ".exe";
                PlatformLibExtension = ".dll";
            }

            // Get Path to Airport CEO and Steam based on Operating System
            this.SteamPath = FindPaths().Item1;
            this.ACEOPath = FindPaths().Item2;
            Logger.Log("Found Steam Installation: " + this.ACEOPath);

            this.ACEOExec = this.ACEOPath + "/Airport CEO" + this.PlatformExecExtension;
            if (!File.Exists(this.ACEOExec))
                throw new Exception("Could not find Airport CEO executable");
            Logger.Log("Found Airport CEO Executable: " + this.ACEOExec);
            this.AssemblyPath = this.ACEOPath + "/Airport CEO_Data/Managed/Assembly-CSharp" + this.PlatformLibExtension;
            this.ModLoaderLibPath = this.ACEOPath + "/ModLoader/MLL" + this.PlatformLibExtension;
            if (!File.Exists(this.ModLoaderLibPath))
                throw new Exception("Missing ModLoaderLibrary" + this.PlatformLibExtension +
                                    ", ModLoader not installed correctly.");
        }

        private Tuple<string, string> FindPaths()
        {
            // WINDOWS LOCATION
            if (CurrentPlatform == Platforms.Windows)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    if (key.GetValue("SteamPath") == null)
                        throw new Exception("Steam installation not found in registry.");
                    else
                    {
                        return new Tuple<string, string>(
                            key.GetValue("SteamPath").ToString(), 
                            key.GetValue("SteamPath").ToString() + "/steamapps/common/Airport CEO"
                        );
                    }
                }
            }
            // MAC OSX LOCATION
            else if (CurrentPlatform == Platforms.OSX)
            {
                // I'm just gonna assume it's the default location, I don't know much about OSX
                return new Tuple<string, string>(
                    "/Applications",
                    "~/Library/Application Support/Steam/SteamApps/common/Airport CEO"
                );
            }
            else
                throw new Exception("Platform not supported.");
        }

        public void BackupAssembly()
        {
            Logger.Log("Backing up files...");            
            // Missing Assembly-CSharp, that's bad yo
            if (!File.Exists(AssemblyPath))
                throw new Exception("Airport CEO Installation Missing Critical File.");

            // If the backup already exists, delete it
            if (File.Exists(ACEOPath + "/Airport CEO_Data/Managed/Assembly-CSharpBACKUP" + this.PlatformLibExtension))
                File.Delete(ACEOPath + "/Airport CEO_Data/Managed/Assembly-CSharpBACKUP" + this.PlatformLibExtension);

            File.Copy(AssemblyPath, ACEOPath + "/Airport CEO_Data/Managed/Assembly-CSharpBACKUP" + this.PlatformLibExtension);
        }

        public void PatchAssembly()
        {
            Logger.Log("Patching Airport CEO Libraries...");
            ModuleDefinition module = ModuleDefinition.ReadModule(AssemblyPath);
            
            // We're patching the MainMenuUI's Start() function for the Entry() function in the library
            // Correction, we're now patching the MainMenuUI's Awake() function instead at the BEGINNING
            TypeDefinition MainMenuUIPatch = module.Types.Single(x => x.Name == "MainMenuUI");
            MethodDefinition startFunction = MainMenuUIPatch.Methods.Single(x => x.Name == "Awake");
            var processor = startFunction.Body.GetILProcessor();

            int ILCounter = 0;
            // load the ModLoaderLibrary Assembly, 2 IL commands
            processor.InsertAfter(startFunction.Body.Instructions[ILCounter], processor.Create(OpCodes.Ldstr, ModLoaderLibPath));
            ILCounter++;
            processor.InsertAfter(startFunction.Body.Instructions[ILCounter], processor.Create(OpCodes.Call, module.ImportReference(typeof(Assembly).GetMethod("LoadFile", new [] { typeof(string) }))));
            ILCounter++;
            Logger.Log("Assembly Loaded.");
            
            // get the ModLoader class in the assembly and call the static method, 8 IL commands
            processor.InsertAfter(startFunction.Body.Instructions[ILCounter], processor.Create(OpCodes.Ldstr, "ModLoaderLibrary.ModLoader"));
            ILCounter++;
            processor.InsertAfter(startFunction.Body.Instructions[ILCounter], processor.Create(OpCodes.Callvirt, module.ImportReference(typeof(Assembly).GetMethod("GetType", new [] { typeof(string) }))));
            ILCounter++;
            processor.InsertAfter(startFunction.Body.Instructions[ILCounter], processor.Create(OpCodes.Ldstr, "Entry"));
            ILCounter++;
            processor.InsertAfter(startFunction.Body.Instructions[ILCounter], processor.Create(OpCodes.Callvirt, module.ImportReference(typeof(System.Type).GetMethod("GetMethod", new[] { typeof(string) }))));
            ILCounter++;
            processor.InsertAfter(startFunction.Body.Instructions[ILCounter], processor.Create(OpCodes.Ldnull));
            ILCounter++;
            processor.InsertAfter(startFunction.Body.Instructions[ILCounter], processor.Create(OpCodes.Ldnull));
            ILCounter++;
            processor.InsertAfter(startFunction.Body.Instructions[ILCounter], processor.Create(OpCodes.Callvirt, module.ImportReference(typeof(MethodBase).GetMethod("Invoke", new[] { typeof(object), typeof(object[]) }))));
            ILCounter++;
            processor.InsertAfter(startFunction.Body.Instructions[ILCounter], processor.Create(OpCodes.Pop));
            ILCounter++;
            Logger.Log("Entrypoint injected.");

            // We're patching the Utils's QuitGame() function for the Exit() function in the library
            TypeDefinition UtilsPatch = module.Types.Single(x => x.Name == "Utils");
            MethodDefinition exitFunction = UtilsPatch.Methods.Single(x => x.Name == "QuitGame");
            processor = exitFunction.Body.GetILProcessor();

            // Call the static method in the ModLoader class
            processor.InsertBefore(getEnd(exitFunction), processor.Create(OpCodes.Ldstr, ModLoaderLibPath));
            processor.InsertBefore(getEnd(exitFunction), processor.Create(OpCodes.Call, module.ImportReference(typeof(Assembly).GetMethod("LoadFile", new[] { typeof(string) }))));
            Logger.Log("Assembly Loaded.");

            processor.InsertBefore(getEnd(exitFunction), processor.Create(OpCodes.Ldstr, "ModLoaderLibrary.ModLoader"));
            processor.InsertBefore(getEnd(exitFunction), processor.Create(OpCodes.Callvirt, module.ImportReference(typeof(Assembly).GetMethod("GetType", new[] { typeof(string) }))));
            processor.InsertBefore(getEnd(exitFunction), processor.Create(OpCodes.Ldstr, "Exit"));
            processor.InsertBefore(getEnd(exitFunction), processor.Create(OpCodes.Callvirt, module.ImportReference(typeof(System.Type).GetMethod("GetMethod", new[] { typeof(string) }))));
            processor.InsertBefore(getEnd(exitFunction), processor.Create(OpCodes.Ldnull));
            processor.InsertBefore(getEnd(exitFunction), processor.Create(OpCodes.Ldnull));
            processor.InsertBefore(getEnd(exitFunction), processor.Create(OpCodes.Callvirt, module.ImportReference(typeof(MethodBase).GetMethod("Invoke", new[] { typeof(object), typeof(object[]) }))));
            processor.InsertBefore(getEnd(exitFunction), processor.Create(OpCodes.Pop));
            Logger.Log("Exitpoint injected.");
            

            // Finished, write to file
            module.Write(AssemblyPath + "PATCHED");
            Logger.Log("File Written.");
            module.Dispose();
        }

        private Instruction getEnd(MethodDefinition def)
        {
            return def.Body.Instructions[def.Body.Instructions.Count - 1];
        }

        public void ReplaceAssembly()
        {
            Logger.Log("Attempting to replace...");
            File.Delete(AssemblyPath);
            File.Move(AssemblyPath + "PATCHED", AssemblyPath);
            Logger.Log("File replaced.");
        }

        public void RunMonoCerts()
        {
            Logger.Log("Copying Mono Certificate Import Dependencies...");
            File.Copy(this.ACEOPath + "/Airport CEO_Data/Managed/Mono.Security" + this.PlatformLibExtension, 
                this.ACEOPath + "/ModLoader/Mono.Security" + this.PlatformLibExtension, true);
            try
            {
                Logger.Log("Running Mozroots...");
                var proc = System.Diagnostics.Process.Start(this.ACEOPath + "/ModLoader/mozroots.exe", "--import --sync");
                proc.WaitForExit();
                Logger.Log("Finished.");
            }
            catch (Exception e)
            {
                Logger.Log("Failed to run mozroots, " + e.Message, Logger.LogType.WARNING);
            }
        }

        public void RevertState()
        {
            Logger.Log("Reverting Changes...");
            File.Delete(AssemblyPath);
            File.Move(ACEOPath + "/Airport CEO_Data/Managed/Assembly-CSharpBACKUP" + PlatformLibExtension, AssemblyPath);
        }
    }
}