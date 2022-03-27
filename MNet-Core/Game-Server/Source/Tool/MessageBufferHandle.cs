using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public abstract class MessageBufferHandle
    {
        internal abstract void Write(NetworkWriter writer);
    }

    public class MessageBufferHandle<T> : MessageBufferHandle
    {
        public T Target;

        internal override void Write(NetworkWriter writer)
        {
            writer.Write(typeof(T));
            writer.Write(Target);
        }

        public MessageBufferHandle(T target)
        {
            this.Target = target;
        }
    }
}