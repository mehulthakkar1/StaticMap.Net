using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace StaticMapUtility
{
    public class StaticMapUtility
    {
        /// <summary>
        /// This is migrated code from php
        /// Any time facing problem in execution, visit main code at https://github.com/esripdx/Static-Maps-API-PHP/blob/master/img.php
        /// </summary>

        const short TILE_SIZE = 256;
        string tileURL;
        double latitude, longitude;
        short zoom = 0;
        short width, height;
        double leftEdge;
        double topEdge;
        WebMercator webmercator = new WebMercator();
        List<Dictionary<string, string>> markers = new List<Dictionary<string, string>>();
        Dictionary<string, string> properties;

        public StaticMapUtility(string tileServerUrl = "")
        {
            tileURL = tileServerUrl;
        }

        List<Dictionary<string, string>> pathProps = new List<Dictionary<string, string>>();
        Dictionary<string, List<Coordinate>> paths = new Dictionary<string, List<Coordinate>>();

        Dictionary<string, List<string>> tileServices = new Dictionary<string, List<string>>{
          {"streets" , new List<string> {"http://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{Z}/{Y}/{X}" }},
          {"satellite" , new List<string> {
            "http://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{Z}/{Y}/{X}"
          }},
          {"hybrid" , new List<string> {
            "http://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{Z}/{Y}/{X}",
            "http://server.arcgisonline.com/ArcGIS/rest/services/Reference/World_Boundaries_and_Places/MapServer/tile/{Z}/{Y}/{X}"
          }},
          {"topo" , new List<string> {
            "http://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{Z}/{Y}/{X}"
          }},
          {"gray" , new List<string> {
            "http://server.arcgisonline.com/ArcGIS/rest/services/Canvas/World_Light_Gray_Base/MapServer/tile/{Z}/{Y}/{X}",
            "http://server.arcgisonline.com/ArcGIS/rest/services/Canvas/World_Light_Gray_Reference/MapServer/tile/{Z}/{Y}/{X}"
          }},
          {"gray-background" , new List<string> {
            "http://server.arcgisonline.com/ArcGIS/rest/services/Canvas/World_Light_Gray_Base/MapServer/tile/{Z}/{Y}/{X}",
          }},
          {"oceans" , new List<string> {
            "http://server.arcgisonline.com/ArcGIS/rest/services/Ocean_Basemap/MapServer/tile/{Z}/{Y}/{X}"
          }},
          {"national-geographic" , new List<string> {
            "http://server.arcgisonline.com/ArcGIS/rest/services/NatGeo_World_Map/MapServer/tile/{Z}/{Y}/{X}"
          }},
          {"osm" , new List<string> {
            "http://tile.openstreetmap.org/{Z}/{X}/{Y}.png"
          }},
          {"stamen-toner" , new List<string> {
            "http://tile.stamen.com/toner/{Z}/{X}/{Y}.png"
          }},
          {"stamen-toner-background" , new List<string> {
            "http://tile.stamen.com/toner-background/{Z}/{X}/{Y}.png"
          }},
          {"stamen-toner-lite" , new List<string> {
            "http://tile.stamen.com/toner-lite/{Z}/{X}/{Y}.png"
          }},
          {"stamen-terrain" , new List<string> {
            "http://tile.stamen.com/terrain/{Z}/{X}/{Y}.png"
          }},
          {"stamen-terrain-background" , new List<string> {
            "http://tile.stamen.com/terrain-background/{Z}/{X}/{Y}.png"
          }},
          {"stamen-watercolor" , new List<string> {
            "http://tile.stamen.com/watercolor/{Z}/{X}/{Y}.png"
          }}
        };
        // If any markers are specified, choose a default lat/lng as the center of all the markers
        Dictionary<string, double> bounds = new Dictionary<string, double>{
          {"minLat" , 90},
          {"maxLat" , -90},
          {"minLng" , 180},
          {"maxLng" , -180}
        };

        public void GetMarkers(HttpRequestBase httpRequest)
        {
            NameValueCollection queryStringColl = httpRequest.QueryString;
            Dictionary<string, string> properties;
            if (queryStringColl["marker"] != null)
            {
                string[] markerColl = queryStringColl.GetValues("marker");

                foreach (string marker in markerColl)
                {
                    properties = new Dictionary<string, string>();
                    Regex expr = new Regex(@"(?<k>[a-z]+):(?<v>[^;]+)");
                    MatchCollection keyValuePairs = expr.Matches(marker);

                    foreach (Match match in keyValuePairs)
                    {

                        properties.Add(match.Groups["k"].Value, match.Groups["v"].Value);
                    }

                    if (properties.ContainsKey("icon") && properties.ContainsKey("lat") && properties.ContainsKey("lng"))
                    {
                        properties.Add("iconImg", HttpContext.Current.Server.MapPath("~/content/images/markers/" + properties["icon"] + ".png"));
                    }

                    if (properties.ContainsKey("iconImg"))
                    {
                        markers.Add(properties);


                        if (double.Parse(properties["lat"]) < bounds["minLat"])
                            bounds["minLat"] = double.Parse(properties["lat"]);
                        if (double.Parse(properties["lat"]) > bounds["maxLat"])
                            bounds["maxLat"] = double.Parse(properties["lat"]);
                        if (double.Parse(properties["lng"]) < bounds["minLng"])
                            bounds["minLng"] = double.Parse(properties["lng"]);
                        if (double.Parse(properties["lng"]) > bounds["maxLng"])
                            bounds["maxLng"] = double.Parse(properties["lng"]);

                    }
                }
            }

        }

        public void GetPaths(HttpRequestBase httpRequest)
        {
            NameValueCollection queryStringColl = httpRequest.QueryString;

            if (queryStringColl["path"] != null)
            {
                int pathCount = 0;

                string[] pathColl = queryStringColl.GetValues("path");

                foreach (string path in pathColl)
                {
                    properties = new Dictionary<string, string>();
                    Regex expr = new Regex(@"(?<k>[a-z]+):(?<v>[^;]+)");
                    MatchCollection keyValuePairs = expr.Matches(path);

                    foreach (Match match in keyValuePairs)
                    {
                        properties.Add(match.Groups["k"].Value, match.Groups["v"].Value);
                    }

                    // Set default color and weight if none specified
                    if (!properties.ContainsKey("color"))
                        properties.Add("color", "333333");
                    if (!properties.ContainsKey("weight"))
                        properties.Add("weight", "3");
                    List<Coordinate> coords = null;

                    if (properties.ContainsKey("enc"))
                    {
                        coords = GoogleMapsUtil.Decode(properties["enc"]).ToList();
                        if (coords.Count > 0)
                        {
                            ++pathCount;
                            foreach (Coordinate coord in coords)
                            {
                                if (double.Parse(coord.Latitude) < bounds["minLat"])
                                    bounds["minLat"] = double.Parse(coord.Latitude);
                                if (double.Parse(coord.Latitude) > bounds["maxLat"])
                                    bounds["maxLat"] = double.Parse(coord.Latitude);
                                if (double.Parse(coord.Longitude) < bounds["minLng"])
                                    bounds["minLng"] = double.Parse(coord.Longitude);
                                if (double.Parse(coord.Longitude) > bounds["maxLng"])
                                    bounds["maxLng"] = double.Parse(coord.Longitude);

                            }
                        }
                    }
                    else
                    {
                        // Now parse the points into an array
                        Regex pathexpr = new Regex(@"\[(?<lat>[0-9\.-]+),(?<lng>[0-9\.-]+)\]");

                        MatchCollection latlngColl = pathexpr.Matches(path);

                        if (latlngColl.Count > 0)
                        {
                            ++pathCount;
                            coords = new List<Coordinate>();
                        }
                        // Adjust the bounds to fit the path
                        foreach (Match point in latlngColl)
                        {
                            coords.Add(new Coordinate(point.Groups["lat"].Value, point.Groups["lng"].Value));
                            if (double.Parse(point.Groups["lat"].Value) < bounds["minLat"])
                                bounds["minLat"] = double.Parse(point.Groups["lat"].Value);
                            if (double.Parse(point.Groups["lat"].Value) > bounds["maxLat"])
                                bounds["maxLat"] = double.Parse(point.Groups["lat"].Value);
                            if (double.Parse(point.Groups["lng"].Value) < bounds["minLng"])
                                bounds["minLng"] = double.Parse(point.Groups["lng"].Value);
                            if (double.Parse(point.Groups["lng"].Value) > bounds["maxLng"])
                                bounds["maxLng"] = double.Parse(point.Groups["lng"].Value);

                        }
                    }
                    paths.Add(pathCount.ToString(), coords);
                    properties.Add("path", pathCount.ToString());
                    pathProps.Add(properties);

                }
            }
        }

        public void GetOtherQueryStringParams(HttpRequestBase httpRequest)
        {
            NameValueCollection queryStringColl = httpRequest.QueryString;

            var defaultLatitude = bounds["minLat"] + ((bounds["maxLat"] - bounds["minLat"]) / 2);
            var defaultLongitude = bounds["minLng"] + ((bounds["maxLng"] - bounds["minLng"]) / 2);
            if (httpRequest["latitude"] != null)
            {
                latitude = double.Parse(httpRequest["latitude"].ToString());
                longitude = double.Parse(httpRequest["longitude"].ToString());
            }
            else
            {
                latitude = defaultLatitude;
                longitude = defaultLongitude;
            }


            width = httpRequest["width"] == null ? (short)300 : short.Parse(httpRequest["width"]);
            height = httpRequest["height"] == null ? (short)300 : short.Parse(httpRequest["height"]);

            // If no zoom is specified, choose a zoom level that will fit all the markers and the path
            if (httpRequest["zoom"] != null)
            {
                zoom = short.Parse(httpRequest["zoom"]);
            }
            else
            {
                // start at max zoom level (18)
                short fitZoom = 18;
                var doesNotFit = true;
                while (fitZoom > 1 && doesNotFit)
                {
                    var center = webmercator.LatLngToPixels(latitude, longitude, fitZoom);
                    leftEdge = center["x"] - width / 2;
                    topEdge = center["y"] - height / 2;
                    // check if the bounding rectangle fits within width/height
                    var sw = webmercator.LatLngToPixels(bounds["minLat"], bounds["minLng"], fitZoom);
                    var ne = webmercator.LatLngToPixels(bounds["maxLat"], bounds["maxLng"], fitZoom);
                    var fitHeight = Math.Abs(ne["y"] - sw["y"]);
                    var fitWidth = Math.Abs(ne["x"] - sw["x"]);
                    if (fitHeight <= height && fitWidth <= width)
                    {
                        doesNotFit = false;
                    }
                    fitZoom--;
                }
                zoom = fitZoom;
            }

            //First check tileUrl from constructor 
            if (string.IsNullOrWhiteSpace(tileURL))
            {
                //if tileUrl is not in constructor, try to get from querystring
                if (httpRequest["basemap"] != null && tileServices.ContainsKey(httpRequest["basemap"]))
                {
                    tileURL = tileServices[httpRequest["basemap"]][0];
                    if (tileServices[httpRequest["basemap"]].Count > 1)
                    {
                    }
                    else
                    {
                    }
                }
                else
                {
                    //if tileUrl is not in constructor and not in querystring, get default from defined list
                    tileURL = tileServices["osm"][0];
                }

            }
        }

        public string UrlForTile(double x, double y, short z, string tileURL)
        {
            tileURL = tileURL.Replace("{x}", x.ToString()).Replace("{y}", y.ToString()).Replace("{z}", z.ToString());
            return tileURL.Replace("{X}", x.ToString()).Replace("{Y}", y.ToString()).Replace("{Z}", z.ToString());
        }

        public Bitmap CreateImage(HttpRequestBase httpRequest)
        {
            GetMarkers(httpRequest);
            GetPaths(httpRequest);
            GetOtherQueryStringParams(httpRequest);

            var center = webmercator.LatLngToPixels(latitude, longitude, zoom);

            var leftEdge = center["x"] - width / 2;
            var topEdge = center["y"] - height / 2;
            var tilePos = webmercator.PixelsToTile(center["x"], center["y"]);

            var pos = webmercator.PositionInTile(center["x"], center["y"]);
            var neTile = webmercator.PixelsToTile(center["x"] + width / 2, center["y"] + height / 2);
            var swTile = webmercator.PixelsToTile(center["x"] - width / 2, center["y"] - height / 2);

            // Now download all the tiles
            var tiles = new Dictionary<string, Dictionary<string, Stream>>();
            var urls = new Dictionary<string, Dictionary<string, string>>();
            var numTiles = 0;

            for (double x = swTile["x"]; x <= neTile["x"]; x++)
            {
                if (!tiles.ContainsKey(x.ToString()))
                {
                    tiles.Add(x.ToString(), new Dictionary<string, Stream>());
                    urls.Add(x.ToString(), new Dictionary<string, string>());
                }
                for (double y = swTile["y"]; y <= neTile["y"]; y++)
                {
                    var url = UrlForTile(x, y, zoom, tileURL);
                    tiles[x.ToString()].Add(y.ToString(), null);
                    urls[x.ToString()].Add(y.ToString(), url);
                    numTiles++;
                }
            }

            foreach (var rowUrls in urls)
            {
                foreach (var url in rowUrls.Value)
                {
                    try
                    {
                        tiles[rowUrls.Key][url.Key] = System.Net.WebRequest.Create(url.Value).GetResponse().GetResponseStream();
                    }
                    catch (Exception)
                    {

                    }

                }
            }

            // Assemble all the tiles into a new image positioned as appropriate

            Bitmap main = new Bitmap(width, height);
            Graphics graphics = Graphics.FromImage(main);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (var ytiles in tiles)
            {
                foreach (var ytile in ytiles.Value)
                {
                    var x = int.Parse(ytiles.Key);
                    var y = int.Parse(ytile.Key);
                    var ox = ((x - tilePos["x"]) * TILE_SIZE) - pos["x"] + (width / 2);
                    var oy = ((y - tilePos["y"]) * TILE_SIZE) - pos["y"] + (height / 2);
                    if (ytile.Value != null)
                    {
                        graphics.DrawImage(Image.FromStream(ytile.Value), Convert.ToSingle(ox), Convert.ToSingle(oy), 256.0f, 256.0f);
                    }

                }
            }

            if (paths.Count > 0)
            {
                // Draw the path with ImageMagick because GD sucks as anti-aliased lines

                foreach (var path in pathProps)
                {
                    Color color = ColorTranslator.FromHtml("#" + path["color"]);
                    Pen pen = new Pen(color, Convert.ToSingle(path["weight"]));
                    List<Point> points = new List<Point>();

                    for (int i = 0; i < paths[path["path"]].Count; i++)
                    {
                        var to = webmercator.LatLngToPixels(double.Parse(paths[path["path"]][i].Latitude), double.Parse(paths[path["path"]][i].Longitude), zoom);
                        points.Add(new Point(Convert.ToInt32(to["x"] - leftEdge), Convert.ToInt32(to["y"] - topEdge)));
                    }
                    graphics.DrawPolygon(pen, points.ToArray());
                }

            }

            // Add markers
            string markerImg = string.Empty;
            foreach (var marker in markers)
            {
                // Icons with a shadow are centered at the bottom middle pixel.
                // Icons with no shadow are centered in the center pixel.
                var px = webmercator.LatLngToPixels(double.Parse(marker["lat"]), double.Parse(marker["lng"]), zoom);
                markerImg = marker["iconImg"];
                Image img = Image.FromFile(markerImg);
                float x = Convert.ToSingle(px["x"] - leftEdge - Math.Round(img.Width / 2.0));
                float y = Convert.ToSingle(px["y"] - topEdge - img.Height);
                graphics.DrawImage(img, x, y);
            }

            return main;
        }

    }
}
