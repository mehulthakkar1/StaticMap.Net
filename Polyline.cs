using StaticMap.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticMap.NET
{
    public class Polyline 
    {
        public List<Coordinate> Coordinates
        {
            get;
            set;
        }

        public Dictionary<string, string> Properties { get; set; }
        public string Color { get; set; }
        public string Weight { get; set; }

        public string Key { get; set; }

    }
}
