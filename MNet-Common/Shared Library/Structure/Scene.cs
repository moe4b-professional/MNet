using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MNet
{
    [Preserve]
    public enum NetworkSceneLoadMode : byte
    {
        Single = 0,
        Additive = 1
    }
}