using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public class InputDispatcher
    {
        delegate bool CallbackDelegate(string command);
        List<CallbackDelegate> Callbacks;

        public bool Invoke(string command)
        {
            for (int i = 0; i < Callbacks.Count; i++)
                if (Callbacks[i].Invoke(command))
                    return true;

            return false;
        }

        public delegate void CommandDelegate(Request request);
        public void Register(string guide, CommandDelegate handler)
        {
            Callbacks.Add(Surrogate);
            bool Surrogate(string command)
            {
                if (command.StartsWith(guide, StringComparison.OrdinalIgnoreCase) == false)
                    return false;

                var split = command.AsSpan().Slice(guide.Length).Trim().ToString();
                var parameters = Argument.Parse(split);

                var request = new Request(parameters);

                try
                {
                    handler(request);
                }
                catch (Exception ex)
                {
                    Log.Info($"Exception Thrown When Executing Command '{command}'" +
                        $"{Environment.NewLine}" +
                        $"Expception: {ex}");
                }

                return true;
            }
        }

        public class Request
        {
            public IList<Argument> Arguments { get; private set; }
            public Argument this[int index] => Arguments[index];

            public Request(IList<Argument> arguments)
            {
                this.Arguments = arguments;
            }
        }

        public struct Argument
        {
            public string Text { get; private set; }

            public T Parse<T>(Func<string, T> parser) => parser(Text);

            public Argument(string text)
            {
                this.Text = text;
            }

            const char BlockCharacter = '"';

            public static List<Argument> Parse(string text)
            {
                var list = new List<Argument>();

                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith(BlockCharacter))
                    {
                        var end = FindEndBlock(parts, i + 1);
                        var count = end - i + 1;

                        var enrtry = string.Join(' ', parts, i, count).Trim(BlockCharacter, ' ');

                        var parameter = new Argument(enrtry);
                        list.Add(parameter);

                        i = end;
                    }
                    else
                    {
                        var parameter = new Argument(parts[i]);
                        list.Add(parameter);
                    }
                }

                static int FindEndBlock(string[] parts, int start)
                {
                    for (int i = start; i < parts.Length; i++)
                        if (parts[i].EndsWith(BlockCharacter))
                            return i;

                    throw new InvalidOperationException("Couldn't Find Next End Block");
                }

                return list;
            }
        }

        public InputDispatcher()
        {
            Callbacks = new List<CallbackDelegate>();
        }
    }
}