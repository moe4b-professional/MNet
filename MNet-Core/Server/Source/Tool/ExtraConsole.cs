using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    public static class ExtraConsole
    {
        public static int Left
        {
            get => Console.CursorLeft;
            set => Console.CursorLeft = value;
        }
        public static int Top
        {
            get => Console.CursorTop;
            set => Console.CursorTop = value;
        }

        static readonly object SyncLock = new object();

        public static void Write(object target) => Write(target, ConsoleColor.White);
        public static void Write(object target, ConsoleColor color)
        {
            lock (SyncLock)
            {
                Input.Clean();

                Console.ForegroundColor = color;
                Console.WriteLine(target);
                Console.ResetColor();

                Input.Write();
            }
        }

        public static class Input
        {
            public static ConsoleColor Color { get; set; } = ConsoleColor.Green;

            static List<char> List;

            internal static void Clean()
            {
                Left = 0;

                for (int i = 0; i < Console.BufferWidth - 1; i++)
                    Console.Write(' ');

                Left = 0;
            }
            internal static void Write()
            {
                Console.ForegroundColor = Color;

                Left = 0;

                Console.Write("> ");

                for (int i = 0; i < List.Count; i++)
                    Console.Write(List[i]);

                Console.ResetColor();
            }

            internal static void Refresh()
            {
                Clean();
                Write();
            }

            public static string Read()
            {
                lock (SyncLock) Refresh();

                while (true)
                {
                    var key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (List.Count > 0)
                            List.RemoveAt(List.Count - 1);
                    }
                    else
                    {
                        List.Add(key.KeyChar);
                    }

                    lock (SyncLock) Refresh();
                }

                Clean();

                var output = new StringBuilder();

                for (int i = 0; i < List.Count; i++)
                    output.Append(List[i]);

                List.Clear();

                return output.ToString();
            }

            static Input()
            {
                List = new List<char>();
            }
        }
        public static string Read() => Input.Read();
    }

    public static class ExtraConsoleLog
    {
        public static void Bind()
        {
            ExtraConsole.Input.Color = ConsoleColor.Green;

            Log.Minimum = Log.Level.Info;
            Log.Output = Output;
        }

        static void Output(object target, Log.Level level)
        {
            var color = LevelToColor(level);

            ExtraConsole.Write($"[{Log.TimeStamp}] {target}", color);
        }

        static ConsoleColor LevelToColor(Log.Level level)
        {
            switch (level)
            {
                case Log.Level.Trace:
                    return ConsoleColor.White;

                case Log.Level.Info:
                    return ConsoleColor.White;

                case Log.Level.Warning:
                    return ConsoleColor.DarkYellow;

                case Log.Level.Error:
                    return ConsoleColor.DarkRed;
            }

            throw new NotImplementedException();
        }
    }
}