using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public static class Log
    {
        public static Level Minimum { get; set; } = Level.Trace;

        public static void Warning(object target) => Add(target, Level.Warning);
        public static void Error(object target) => Add(target, Level.Error);
        public static void Info(object target) => Add(target, Level.Info);
        public static void Trace(object target) => Add(target, Level.Trace);

        public static string TimeStamp => DateTime.Now.ToString("hh:mm:ss");

        public delegate void OutputDelegate(object target, Level level);
        public static OutputDelegate Output { get; set; }
        public static void Add(object target, Level level)
        {
            if (level < Minimum)
                return;

            if (Output == null)
                Console.WriteLine($"[{TimeStamp}] {level}: {target}");
            else
                Output(target, level);
        }

        public enum Level
        {
            Trace,
            Info,
            Warning,
            Error
        }
    }
}