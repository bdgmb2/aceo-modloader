using System;
using System.IO;

namespace ModLoader
{
    class Logger
    {
        public enum LogType { INFO, WARNING, ERROR };
        private static TextWriter fileHandle;
        private static bool setup = false;

        public static void SetupLogger(string fileName)
        {
            // This thing didn't work anyway
            //fileHandle = File.CreateText(fileName);
            setup = true;
        }

        public static void Log(string message, LogType type = LogType.INFO)
        {
            if (!setup)
                throw new Exception("Logger not set up before calling Log()");

            string fullMessage = "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() +
                                 "] [" + type + "] " + message;

            //fileHandle.WriteLine(fullMessage);
            Console.WriteLine(fullMessage);
        }
    }
}
