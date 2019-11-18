using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticMapUtility
{
    public class Coordinate
    {
        public Coordinate() { }

        public Coordinate(string lat, string lng)
            : this()
        {
            Latitude = lat;
            Longitude = lng;
        }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
}
