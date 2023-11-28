using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BrewLib.Audio
{
    internal static class NativeBass
    {
        internal static void SetBassDllPath() { } // ensures the static initializer is run exactly once

        static NativeBass()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            var platform = Environment.Is64BitProcess ? "x64" : "x86";
            SetDllDirectory(Path.Combine(currentDirectory, platform));
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string path);
    }
}
