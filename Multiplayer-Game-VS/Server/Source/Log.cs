﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Server
{
    public static class Log
    {
        public static void Info(object target)
        {
            Console.WriteLine(target);
        }
    }
}