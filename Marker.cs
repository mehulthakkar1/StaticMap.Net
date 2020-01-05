using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StaticMap.Net;

namespace StaticMap.NET
{
    public class Marker 
    {
        public Coordinate Coordinate
        {
            get;
            set;
        }

        public Dictionary<string, string> Properties { get; set; }

        public string IconImg { get; set; }
    }
}
