# StaticMap.Net
A C# library to generate static map

## How to use
Add the StaticMap.Net dll in your project.

Add StaticMapController in your project.

```
public class StaticMapController : Controller
{
    // GET: StaticMap
    public ActionResult Index()
    {
        var map = new StaticMap.Net.StaticMap();
        var image = map.CreateImage(HttpContext.Request);
        using (var stream = new MemoryStream())
        {
            image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return File(stream.ToArray(), "image/jpeg");
        }
    }
}
```

An example to add map in View

```
<img src="~/staticmap?basemap=national-geographic&width=400&height=240&zoom=14&markers=lat:45.5165;lng:-122.6764;icon:marker-standard" />
<img src="~/staticmap?basemap=gray&width=400&height=240&&path=[37.39561,-122.08952],[37.39125,-122.07064],[37.40025,-122.06188],[37.40998,-122.10291],[37.39561,-122.08952]" />
```

## Parameters
Parameters can be sent in the query string .

* zoom - optional - Set the zoom level for the map. If not specified, a zoom level will be chosen that contains all markers on the map.
* width - default 300 - Width in pixels of the final image
* height - default 300 - Height in pixels of the final image
* basemap - default "osm", use StaticMap.GetDefaultTileServices() to get all TileServices. To use any other tile service provider url can be passed in constructor of StaticMap.
* latitude - optional - Latitude to center the map at. Not needed if using the location parameter, or if specifying one or more markers.
* longitude - optional - Longitude to center the map at.
* markers - Specify one or more markers to overlay on the map.
* path - Specify one or more paths to draw on the map. 
