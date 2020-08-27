using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Concurrent;

namespace Backend.Server
{
    public class IDCollection<TType>
        where TType : class
    {
        Dictionary<ushort, TType> types;

        Dictionary<TType, ushort> codes;

        ConcurrentQueue<ushort> vacant;
        
        ushort index;

        object sync = new object();

        public IReadOnlyCollection<TType> Collection => types.Values;

        public int Count => Collection.Count;

        public TType this[ushort code] => types[code];
        public ushort this[TType type] => codes[type];

        public ushort Add(TType type)
        {
            lock (sync)
            {
                if (vacant.TryDequeue(out var code) == false)
                {
                    code = index;

                    index += 1;
                }

                types[code] = type;
                codes[type] = code;

                return code;
            }
        }

        public void Assign(TType type, ushort code)
        {
            lock (sync)
            {
                types[code] = type;
                codes[type] = code;
            }
        }

        public ushort Reserve()
        {
            lock (sync)
            {
                if (vacant.TryDequeue(out var code) == false)
                {
                    code = index;

                    index += 1;
                }

                return code;
            }
        }

        public bool Remove(TType type)
        {
            lock(sync)
            {
                if (codes.TryGetValue(type, out var code) == false) return false;

                types.Remove(code);
                codes.Remove(type);

                vacant.Enqueue(code);

                return true;
            }
        }

        public bool TryGetValue(ushort code, out TType type) => types.TryGetValue(code, out type);

        public bool Contains(TType type) => codes.ContainsKey(type);
        public bool Contains(ushort code) => types.ContainsKey(code);

        public IDCollection()
        {
            types = new Dictionary<ushort, TType>();

            codes = new Dictionary<TType, ushort>();

            vacant = new ConcurrentQueue<ushort>();

            index = 0;
        }
    }
}