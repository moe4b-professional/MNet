using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Shared
{
    public static class Log
    {
        public static void Warning(object target) => Info(target);
        public static void Error(object target) => Info(target);
        public static void Info(object target)
        {
            Console.WriteLine(target);
        }
    }
}