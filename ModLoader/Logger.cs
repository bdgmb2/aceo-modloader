using System;
using System.IO;

namespace ModLoader
{
    static class Logger
    {
        private static StreamWriter _fstream;
        private static bool _ready = false;
        public static bool logDebug { get; private set; }
        private static DateTime _patcherStartTime;

        public enum LogType { Debug, Info, Warning, Error };

        public static void Initialize(bool logDebugIn = false, string filename = "modloader-log.txt")
        {
            if (!_ready)
            {
                _fstream = new StreamWriter(filename);
                _ready = true;
                logDebug = logDebugIn;
                _patcherStartTime = DateTime.Now;
            }
        }

        private static TimeSpan GetTimeFromStart()
        {
            return DateTime.Now - _patcherStartTime;
        }

        public static void Log(string message, LogType type = LogType.Info)
        {
            if (!_ready)
                Initialize();

            string finalMessage = "[" + GetTimeFromStart().Minutes + ":" + GetTimeFromStart().Seconds + ":" + GetTimeFromStart().Milliseconds + "]";
            switch (type)
            {
                case LogType.Info:
                    finalMessage += "[INFO] " + message;
                    break;
                case LogType.Warning:
                    finalMessage += "[WARNING] " + message;
                    break;
                case LogType.Error:
                    finalMessage += "[ERROR] " + message;
                    break;
                case LogType.Debug:
                    if (logDebug)
                        finalMessage += "[DEBUG] " + message;
                    else return;
                    break;
                default:
                    return;
            }
            Console.WriteLine(finalMessage);
            _fstream.WriteLine(finalMessage);
        }
    }
}
