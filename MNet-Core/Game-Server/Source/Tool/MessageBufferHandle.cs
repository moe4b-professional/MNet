using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    public abstract class MessageBufferHandle
    {
        protected internal abstract object GetTarget();
    }

    public class MessageBufferHandle<T> : MessageBufferHandle
    {
        public T Target;

        protected internal override object GetTarget() => Target;

        public MessageBufferHandle(T target)
        {
            this.Target = target;
        }
    }
}