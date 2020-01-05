using Newtonsoft.Json;
using StaticMap.NET;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

namespace StaticMap.Net
{
    public class StaticMap
    {
        /// <summary>
        /// This is migrated code from php
        /// Any time facing problem in execution, visit main code at https://github.com/esripdx/Static-Maps-API-PHP/blob/master/img.php
        /// </summary>
        const short TILE_SIZE = 256;
        // If any markers are specified, choose a default lat/lng as the center of all the markers
        readonly Dictionary<string, double> bounds;
                
        public StaticMap()
        {
            bounds = new Dictionary<string, double>{
              {"minLat" , 90},
              {"maxLat" , -90},
              {"minLng" , 180},
              {"maxLng" , -180}
            };
        }

        public Dictionary<string, List<string>> GetDefaultTileServices()
        {
            var aAss = Assembly.GetExecutingAssembly();
            var aAssName = aAss.FullName.Split(',')[0];
            var aStream = aAss.GetManifestResourceStream(aAssName + ".TileServices.json");
            var tileServiceList = "";
            using (var aStreamReader = new StreamReader(aStream))
            {
                tileServiceList = aStreamReader.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(tileServiceList);
        }

        public List<Marker> GetMarkers(HttpRequestBase httpRequest)
        {
            var markers = new List<Marker>();
            var queryStringColl = httpRequest.QueryString;
            if (queryStringColl["markers"] != null)
            {
                var markerColl = queryStringColl.GetValues("markers");

                foreach (string marker in markerColl)
                {
                    var properties = new Dictionary<string, string>();
                    var expr = new Regex(@"(?<k>[a-z]+):(?<v>[^;]+)");
                    var keyValuePairs = expr.Matches(marker);

                    foreach (Match match in keyValuePairs)
                    {
                        properties.Add(match.Groups["k"].Value, match.Groups["v"].Value);
                    }
                    var iconImg = "";
                    if (properties.ContainsKey("icon") && properties.ContainsKey("lat") && properties.ContainsKey("lng"))
                    {
                        iconImg = HttpContext.Current.Server.MapPath("~/content/images/markers/" + properties["icon"] + ".png");
                    }

                    if (iconImg != string.Empty)
                    {
                        var m = new Marker {
                            Coordinate = new Coordinate {
                                Latitude = properties["lat"],
                                Longitude = properties["lng"]
                            },
                            Properties = properties,
                            IconImg = iconImg
                        };
                        markers.Add(m);
                        bounds["minLat"] = Math.Min(double.Parse(properties["lat"]), bounds["minLat"]);
                        bounds["maxLat"] = Math.Max(double.Parse(properties["lat"]), bounds["maxLat"]);
                        bounds["minLng"] = Math.Min(double.Parse(properties["lng"]), bounds["minLng"]);
                        bounds["maxLng"] = Math.Max(double.Parse(properties["lng"]), bounds["maxLng"]);
                    }
                }
            }
            return markers;
        }

        public List<Polyline> GetPaths(HttpRequestBase httpRequest)
        {
            var polylines = new List<Polyline>(); ;
            var pathProps = new List<Dictionary<string, string>>();
            var queryStringColl = httpRequest.QueryString;

            if (queryStringColl["path"] != null)
            {
                var pathCount = 0;

                var pathColl = queryStringColl.GetValues("path");

                foreach (var path in pathColl)
                {
                    var polyline = new Polyline();
                    var properties = new Dictionary<string, string>();
                    var expr = new Regex(@"(?<k>[a-z]+):(?<v>[^;]+)");
                    var keyValuePairs = expr.Matches(path);

                    foreach (Match match in keyValuePairs)
                    {
                        properties.Add(match.Groups["k"].Value, match.Groups["v"].Value);
                    }

                    // Set default color and weight if none specified
                    if (!properties.ContainsKey("color"))
                        properties.Add("color", "333333");
                    if (!properties.ContainsKey("weight"))
                        properties.Add("weight", "3");

                    polyline.Color = properties["color"];
                    polyline.Weight = properties["weight"];
                    
                    if (properties.ContainsKey("enc"))
                    {
                        polyline.Coordinates = GoogleMapsUtil.Decode(properties["enc"]).ToList();
                        if (polyline.Coordinates.Count > 0)
                        {
                            ++pathCount;
                            foreach (Coordinate coord in polyline.Coordinates)
                            {
                                bounds["minLat"] = Math.Min(double.Parse(coord.Latitude), bounds["minLat"]);
                                bounds["maxLat"] = Math.Max(double.Parse(coord.Latitude), bounds["maxLat"]);
                                bounds["minLng"] = Math.Min(double.Parse(coord.Longitude), bounds["minLng"]);
                                bounds["maxLng"] = Math.Max(double.Parse(coord.Longitude), bounds["maxLng"]);
                            }
                        }
                    }
                    else
                    {
                        // Now parse the points into an array
                        var pathexpr = new Regex(@"\[(?<lat>[0-9\.-]+),(?<lng>[0-9\.-]+)\]");

                        var latlngColl = pathexpr.Matches(path);

                        if (latlngColl.Count > 0)
                        {
                            ++pathCount;
                            polyline.Coordinates = new List<Coordinate>();
                        }
                        // Adjust the bounds to fit the path
                        foreach (Match point in latlngColl)
                        {
                            polyline.Coordinates.Add(new Coordinate { Latitude = point.Groups["lat"].Value, Longitude = point.Groups["lng"].Value });
                            bounds["minLat"] = Math.Min(double.Parse(point.Groups["lat"].Value), bounds["minLat"]);
                            bounds["maxLat"] = Math.Max(double.Parse(point.Groups["lat"].Value), bounds["maxLat"]);
                            bounds["minLng"] = Math.Min(double.Parse(point.Groups["lng"].Value), bounds["minLng"]);
                            bounds["maxLng"] = Math.Max(double.Parse(point.Groups["lng"].Value), bounds["maxLng"]);
                        }
                    }
                    polyline.Key = pathCount.ToString();
                    properties.Add("path", pathCount.ToString());
                    polyline.Properties = properties;
                    polylines.Add(polyline);
                }
            }
            return polylines;
        }

        public OtherMapProperties GetOtherQueryStringParams(HttpRequestBase httpRequest)
        {
            var otherMapProp = new OtherMapProperties();
            var queryStringColl = httpRequest.QueryString;

            var defaultLatitude = bounds["minLat"] + ((bounds["maxLat"] - bounds["minLat"]) / 2);
            var defaultLongitude = bounds["minLng"] + ((bounds["maxLng"] - bounds["minLng"]) / 2);
            if (httpRequest["latitude"] != null)
            {
                otherMapProp.Latitude = double.Parse(httpRequest["latitude"].ToString());
                otherMapProp.Longitude = double.Parse(httpRequest["longitude"].ToString());
            }
            else
            {
                otherMapProp.Latitude = defaultLatitude;
                otherMapProp.Longitude = defaultLongitude;
            }
            
            otherMapProp.Width = httpRequest["width"] == null ? (short)300 : short.Parse(httpRequest["width"]);
            otherMapProp.Height = httpRequest["height"] == null ? (short)300 : short.Parse(httpRequest["height"]);

            // If no zoom is specified, choose a zoom level that will fit all the markers and the path
            if (httpRequest["zoom"] != null)
            {
                otherMapProp.Zoom = short.Parse(httpRequest["zoom"]);
            }
            else
            {
                // start at max zoom level (18)
                short fitZoom = 18;
                var doesNotFit = true;
                while (fitZoom > 1 && doesNotFit)
                {
                    var center = WebMercator.LatLngToPixels(otherMapProp.Latitude, otherMapProp.Longitude, fitZoom);
                    // check if the bounding rectangle fits within width/height
                    var sw = WebMercator.LatLngToPixels(bounds["minLat"], bounds["minLng"], fitZoom);
                    var ne = WebMercator.LatLngToPixels(bounds["maxLat"], bounds["maxLng"], fitZoom);
                    var fitHeight = Math.Abs(ne["y"] - sw["y"]);
                    var fitWidth = Math.Abs(ne["x"] - sw["x"]);
                    if (fitHeight <= otherMapProp.Height && fitWidth <= otherMapProp.Width)
                    {
                        doesNotFit = false;
                    }
                    fitZoom--;
                }
                otherMapProp.Zoom = fitZoom;
            }

            //First check tileUrl from constructor 
            if (string.IsNullOrWhiteSpace(otherMapProp.TileService))
            {
                var tileServices = GetDefaultTileServices();
                //if tileUrl is not in constructor, try to get from querystring
                if (httpRequest["basemap"] != null && tileServices.ContainsKey(httpRequest["basemap"]))
                {
                    otherMapProp.TileService = tileServices[httpRequest["basemap"]][0];
                }
                else
                {
                    //if tileUrl is not in constructor and not in querystring, get default from defined list
                    otherMapProp.TileService = tileServices["osm"][0];
                }

            }
            return otherMapProp;
        }

        public string UrlForTile(double x, double y, short z, string tileURL)
        {
            tileURL = tileURL.Replace("{x}", x.ToString()).Replace("{y}", y.ToString()).Replace("{z}", z.ToString());
            return tileURL.Replace("{X}", x.ToString()).Replace("{Y}", y.ToString()).Replace("{Z}", z.ToString());
        }

        public Bitmap CreateImage(HttpRequestBase httpRequest)
        {
            var markers = GetMarkers(httpRequest);
            var paths = GetPaths(httpRequest);
            var otherMapProp = GetOtherQueryStringParams(httpRequest);

            var center = WebMercator.LatLngToPixels(otherMapProp.Latitude, otherMapProp.Longitude, otherMapProp.Zoom);

            var leftEdge = center["x"] - otherMapProp.Width / 2;
            var topEdge = center["y"] - otherMapProp.Height / 2;
            var tilePos = WebMercator.PixelsToTile(center["x"], center["y"]);

            var pos = WebMercator.PositionInTile(center["x"], center["y"]);
            var neTile = WebMercator.PixelsToTile(center["x"] + otherMapProp.Width / 2, center["y"] + otherMapProp.Height / 2);
            var swTile = WebMercator.PixelsToTile(center["x"] - otherMapProp.Width / 2, center["y"] - otherMapProp.Height / 2);

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
                    var url = UrlForTile(x, y, otherMapProp.Zoom, otherMapProp.TileService);
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
                    catch (Exception ex)
                    {
                        throw new Exception($"Calling {url.Value} is failed.", ex);
                    }

                }
            }

            // Assemble all the tiles into a new image positioned as appropriate
            var main = new Bitmap(otherMapProp.Width, otherMapProp.Height);
            var graphics = Graphics.FromImage(main);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (var ytiles in tiles)
            {
                foreach (var ytile in ytiles.Value)
                {
                    var x = int.Parse(ytiles.Key);
                    var y = int.Parse(ytile.Key);
                    var ox = ((x - tilePos["x"]) * TILE_SIZE) - pos["x"] + (otherMapProp.Width / 2);
                    var oy = ((y - tilePos["y"]) * TILE_SIZE) - pos["y"] + (otherMapProp.Height / 2);
                    if (ytile.Value != null)
                    {
                        graphics.DrawImage(Image.FromStream(ytile.Value), Convert.ToSingle(ox), Convert.ToSingle(oy), 256.0f, 256.0f);
                    }

                }
            }

            if (paths.Count > 0)
            {
                // Draw the path with ImageMagick because GD sucks as anti-aliased lines
                foreach (var path in paths)
                {
                    var color = ColorTranslator.FromHtml("#" + path.Color);
                    var pen = new Pen(color, Convert.ToSingle(path.Weight));
                    var points = new List<Point>();

                    for (int i = 0; i < path.Coordinates.Count; i++)
                    {
                        var to = WebMercator.LatLngToPixels(double.Parse(path.Coordinates[i].Latitude), double.Parse(path.Coordinates[i].Longitude), otherMapProp.Zoom);
                        points.Add(new Point(Convert.ToInt32(to["x"] - leftEdge), Convert.ToInt32(to["y"] - topEdge)));
                    }
                    graphics.DrawPolygon(pen, points.ToArray());
                }
            }

            // Add markers
            foreach (var marker in markers)
            {
                // Icons with a shadow are centered at the bottom middle pixel.
                // Icons with no shadow are centered in the center pixel.
                var px = WebMercator.LatLngToPixels(double.Parse(marker.Coordinate.Latitude), double.Parse(marker.Coordinate.Longitude), otherMapProp.Zoom);
                var img = Image.FromFile(marker.IconImg);
                float x = Convert.ToSingle(px["x"] - leftEdge - Math.Round(img.Width / 2.0));
                float y = Convert.ToSingle(px["y"] - topEdge - img.Height);
                graphics.DrawImage(img, x, y);
            }

            return main;
        }

    }
}
