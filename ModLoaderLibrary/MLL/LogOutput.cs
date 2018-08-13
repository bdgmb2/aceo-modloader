using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ModLoaderLibrary
{
    class LogOutput
    {
        public enum LogType { INFO, WARNING, ERROR };
        private static System.IO.StreamWriter outFile;
        private static bool ready = false;

        public static void Initialize()
        {
            outFile = new System.IO.StreamWriter("ModLoader/output.log");
            ready = true;
        }

        public static void Log(string message, LogType type = LogType.INFO)
        {
            if (!ready)
                throw new Exception("Logger not initialized!");
            outFile.WriteLine($"[{DateTime.Now.ToLongTimeString()}] [{type}] {message}");
        }

        public static void DisposeFirst()
        {
            ready = false;
            outFile.Close();
        }
    }
}
