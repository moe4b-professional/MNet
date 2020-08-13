using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Constants
{
    public static class RestAPI
    {
        public const int Port = 8080;

        public static class Requests
        {
            public const string ListMatches = nameof(ListMatches);
        }
    }
}
