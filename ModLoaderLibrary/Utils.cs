using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace ModLoaderLibrary
{
    public class Logger
    {
        public enum LogType { Info, Warning, Error, Debug };
        private static StreamWriter _file;
        private static bool _ready = false, _isDebug;
        private static DateTime _gameStartTime;

        public static void Initialize(bool verbosity)
        {
            _file = new StreamWriter("ModLoader/MLLoutput.log");
            _ready = true;
            _isDebug = verbosity;
            _gameStartTime = DateTime.Now;
        }

        private static TimeSpan GetTimeFromStart()
        {
            return DateTime.Now - _gameStartTime;
        }

        public static void Log(string message, LogType type = LogType.Info)
        {
            if (!_ready)
                throw new Exception("Logger not initialized!");

            string finalMessage = "[" + GetTimeFromStart().Minutes + ":" + GetTimeFromStart().Seconds + ":" + GetTimeFromStart().Milliseconds + "]";
            switch (type)
            {
                case LogType.Info:
                    finalMessage += " [INFO] " + message;
                    break;
                case LogType.Warning:
                    finalMessage += " [WARNING] " + message;
                    break;
                case LogType.Error:
                    finalMessage += " [ERROR] " + message;
                    break;
                case LogType.Debug:
                    if (!_isDebug)
                        return;
                    else
                        finalMessage += " [DEBUG] " + message;
                    break;
            }
            _file.WriteLine(finalMessage);
            _file.Flush();
        }

        public static void DisposeFirst()
        {
            _ready = false;
            _file.Close();
        }
    }

    public static class MLUtils
    {
        public static Sprite LoadSpriteFromFile(string modName, string filename, float pixelsPerUnit = 100.0f)
        {
            string fullPath = Path.Combine(GlobalVars.modPath, modName, filename);
            Texture2D texture = null;
            Logger.Log($"Loading sprite from {fullPath}", Logger.LogType.Debug);
            if (File.Exists(fullPath))
            {
                texture = new Texture2D(2, 2);
                texture.LoadImage(File.ReadAllBytes(fullPath));
            }
            if (texture != null)
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0),
                    pixelsPerUnit);
            Logger.Log($"Sprite {filename} from {modName} could not be loaded.", Logger.LogType.Warning);
            return null;
        }

        public static void RestartGameWithML()
        {
            // Add restart file flag so ModLoader knows what to do
            var fs = File.Open(Path.Combine(Application.dataPath, "RESTARTFLAG"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            fs.Close();
            fs.Dispose();
            File.SetLastWriteTimeUtc(Path.Combine(Application.dataPath, "RESTARTFLAG"), DateTime.Now);

            // Quit the game
            Utils.QuitGame();
        }
    }
}