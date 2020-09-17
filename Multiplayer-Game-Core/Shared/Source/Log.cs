using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend
{
    public static class Log
    {
        public static void Warning(object target) => Add(target, Level.Warning);

        public static void Error(object target) => Add(target, Level.Error);

        public static void Info(object target) => Add(target, Level.Info);

        public delegate void OutputDelegate(object target, Level level);
        public static OutputDelegate Output { get; set; }
        static void Add(object target, Level level)
        {
            if (Output == null)
                Console.WriteLine($"{level}: {target}");
            else
                Output(target, level);
        }

        public enum Level
        {
            Info, Warning, Error
        }
    }
}