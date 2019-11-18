using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StaticMapUtility
{
    public class WebMercator
    {
        private const short TILE_SIZE = 256;
        public double TotalPixelsForZoomLevel(short zoom)
        {
            return Math.Pow(2, zoom) * TILE_SIZE;
        }
        public double LngToX(double longitude, short zoom)
        {
            return Math.Round(((longitude + 180) / 360) * TotalPixelsForZoomLevel(zoom));
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts decimal to radians degrees             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts radians to decimal degrees             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);

        }

        public double ATanh(double x)
        {
            return (Math.Log(1 + x) - Math.Log(1 - x)) / 2;
        }

        public double LatToY(double latitude, short zoom)
        {
            return Math.Round(((ATanh(Math.Sin(deg2rad(-latitude))) / Math.PI) + 1) * TotalPixelsForZoomLevel(--zoom));
        }

        public Dictionary<string, double> LatLngToPixels(double latitude, double longitude, short zoom)
        {
            return new Dictionary<string, double>{
                {"x",  LngToX(longitude, zoom)},{"y", LatToY(latitude, zoom)}
            };

        }
        public double XToLng(short x, short zoom)
        {
            return ((x * 360) / TotalPixelsForZoomLevel(zoom)) - 180;
        }

        public double YToLat(short y, short zoom)
        {
            double a = Math.PI * ((y / TotalPixelsForZoomLevel(--zoom)) - 1);
            return -1 * (rad2deg(Math.Asin(Math.Tanh(a))));
        }

        public Dictionary<string, double> PixelsToLatLng(short x, short y, short zoom)
        {
            return new Dictionary<string, double>{
            {"lat", YToLat(y, zoom)},
            {"lng", XToLng(x, zoom)}
          };
        }

        public Dictionary<string, double> TileToPixels(short x, short y)
        {
            return new Dictionary<string, double>{
            {"x" , x * TILE_SIZE},
            {"y", y * TILE_SIZE}
          };
        }

        public Dictionary<string, double> PixelsToTile(double x, double y)
        {
            return new Dictionary<string, double>{
            {"x", Math.Floor(x / TILE_SIZE)},
            {"y", Math.Floor(y / TILE_SIZE)}
          };
        }

        public Dictionary<string, double> PositionInTile(double x, double y)
        {
            Dictionary<string, double> tile = PixelsToTile(x, y);
            return new Dictionary<string, double>{
            {"x", Math.Round(TILE_SIZE * ((x / TILE_SIZE) - tile["x"]))},
            {"y", Math.Round(TILE_SIZE * ((y / TILE_SIZE) - tile["y"]))}
          };
        }
    }

}