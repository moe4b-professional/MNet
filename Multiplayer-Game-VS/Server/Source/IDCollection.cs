using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Concurrent;

namespace Game.Server
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

        public TType this[ushort index] => types[index];
        public ushort this[TType type] => codes[type];

        public ushort Add(TType type)
        {
            lock (sync)
            {
                if (vacant.TryDequeue(out var id) == false)
                {
                    id = index;

                    index += 1;
                }

                types[id] = type;
                codes[type] = id;

                return id;
            }
        }

        public bool Remove(TType type)
        {
            lock(sync)
            {
                if (codes.TryGetValue(type, out var id) == false) return false;

                types[id] = null;
                codes.Remove(type);

                vacant.Enqueue(id);

                return true;
            }
        }

        public bool TryGetValue(ushort id, out TType type) => types.TryGetValue(id, out type);

        public IDCollection()
        {
            types = new Dictionary<ushort, TType>();

            codes = new Dictionary<TType, ushort>();

            vacant = new ConcurrentQueue<ushort>();

            index = 0;
        }
    }
}