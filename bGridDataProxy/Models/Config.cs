using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace bGridDataProxy.Models
{
    public class Azure
    {
        public string StorageKey { get; set; }
    }

    public class bGrid
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Endpoint { get; set; }
    }
}
