using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceRecog.Services
{
    public static class Logger
    {
        public static event Action<string> MessageLogged;

        public static void Log(string message)
        {
            string logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Debug.WriteLine(logMessage);
            MessageLogged?.Invoke(logMessage);
        }
    }
}
