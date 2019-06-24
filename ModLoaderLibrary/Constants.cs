using System;
using System.Collections.Generic;
using System.Reflection;

// MLVersion is used both in ModLoaderLibrary and ModLoader.
public static class MLVersion
{
    public static readonly string Major = "0";
    public static readonly string Minor = "2";
    public static readonly string Revision = "0";
    public new static string ToString()
    {
        return $"{Major}.{Minor}.{Revision}";
    }
}

namespace ModLoaderLibrary
{
    class GlobalVars
    {
        public static string modPath = ""; // Loaded by ModLoaderLibrary in init
        public static int numMods = 0;
        public static List<Tuple<string, Assembly>> modAssemblies = new List<Tuple<string, Assembly>>();
    }
}