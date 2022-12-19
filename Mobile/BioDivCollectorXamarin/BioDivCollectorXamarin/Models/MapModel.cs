using BioDivCollectorXamarin.Models.Wms;
using BioDivCollectorXamarin.ViewModels;
using BioDivCollectorXamarin.Models.MBTiles;
using BruTile;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.Providers;
using Mapsui.Styles;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Xamarin.Essentials;
using Xamarin.Forms;
using static BioDivCollectorXamarin.Models.DataDAO;
using System.Threading.Tasks;
using Mapsui.Utilities;
using System.Net.Http;
using System.Net;
using SQLite;
using BioDivCollectorXamarin.Models.DatabaseModel;
using static BioDivCollectorXamarin.Helpers.Interfaces;

namespace BioDivCollectorXamarin.Models
{
    class MapModel
    {

        /// <summary>
        /// Create an array of map layers: either mbtiles layers if offline and the mbtiles file exists, or direct links to the map servers 
        /// </summary>
        /// <returns>An observable collection of map layers</returns>
        public static async Task<List<MapLayer>> MakeArrayOfLayers()
        {
            int i = 0;

            var dirPath = App.TileLocation;
            //Get the offline layers
            var offlineLayers = GetOfflineLayers(dirPath);
            //Get the online layers
            var layers = await GetLayersForMap(App.CurrentProjectId);
            //Add online wms layers
            var layerStack = layers.OrderByDescending(o => o.order).ToList();

            //Create an array for adding the layers into in an ordered fashion
            var mapLayersTemp = new MapLayer[layers.Count];

            bool noInternet = MapModel.IsAppDisconnected();

            foreach (var layer in layerStack)
            {
                //Now add the layers in their correct order
                try
                {
                    var layerNo = Math.Max(layer.order - 1, 0);
                    bool offlineLayerExists = offlineLayers.Keys.Contains(layer.title);

                    if (layer.fileBased)
                    {
                        //Add mbtiles layers
                        offlineLayers.TryGetValue(layer.wmsLayer, out ILayer offlineLayer);
                        var fileLayer = new MapLayer(layer.Id, layer.visible, layer.order, offlineLayer);
                        fileLayer.Opacity = layer.opacity;
                        fileLayer.Name = layer.title;
                        mapLayersTemp.SetValue(fileLayer, layerNo);
                        i++;

                        Preferences.Set(layer.wmsLayer, fileLayer.ToString());
                        var lay = Preferences.Get(layer.wmsLayer, null);
                        var path = dirPath + "/" + layer.wmsLayer;
                        FileInfo fi = new FileInfo(dirPath + "/" + layer.wmsLayer + ".mbtiles");
                        if (fi != null)
                        {
                            fileLayer.LocalStorage = fi.Length;
                        }
                        else
                        {
                            fileLayer.LocalStorage = 0;
                        }
                    }

                    else if (noInternet && offlineLayerExists)
                    {
                        //If no internet, check for saved tiles
                        ILayer offlineLayer;
                        offlineLayers.TryGetValue(layer.title, out offlineLayer);

                        if (offlineLayer != null)
                        {
                            offlineLayer.Opacity = layer.opacity;
                            offlineLayer.Enabled = layer.visible;
                            var WmsLayer = new MapLayer(layer.Id, true, 0, offlineLayer);
                            WmsLayer.Opacity = layer.opacity;
                            WmsLayer.Enabled = layer.visible;
                            WmsLayer.LayerZ = layer.order;
                            WmsLayer.Name = layer.title;
                            mapLayersTemp.SetValue(WmsLayer, layerNo);
                            var path = dirPath + "/" + layer.title;
                            FileInfo fi = new FileInfo(dirPath + "/" + layer.title + ".mbtiles");
                            if (fi != null)
                            {
                                WmsLayer.LocalStorage = fi.Length;
                            }
                            else
                            {
                                WmsLayer.LocalStorage = 0;
                            }
                            i++;
                        }
                    }
                    else
                    {
                        //If internet, read directly from WMS
                        var layerWms = WMSLayer.CreateWMSLayer(layer.url, layer.wmsLayer, "EPSG:3857", layer.title, layer.layerId.ToString());
                        layerWms.Opacity = layer.opacity;
                        layerWms.Enabled = layer.visible;
                        if (layerWms != null)
                        {
                            var WmsLayer = new MapLayer(layer.Id, true, 0, layerWms);
                            WmsLayer.Opacity = layer.opacity;
                            WmsLayer.Enabled = layer.visible;
                            WmsLayer.LayerZ = layer.order;
                            WmsLayer.Name = layer.title;

                            mapLayersTemp.SetValue(WmsLayer, layerNo);
                            if (offlineLayerExists)
                            {
                                var path = dirPath + "/" + layer.title;
                                FileInfo fi = new FileInfo(dirPath + "/" + layer.title + ".mbtiles");
                                if (fi != null)
                                {
                                    WmsLayer.LocalStorage = fi.Length;
                                }
                                else
                                {
                                    WmsLayer.LocalStorage = 0;
                                }

                            }
                            i++;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            var mapLayersTempList = mapLayersTemp.ToList().GetRange(0, i);

            return mapLayersTempList;

        }

        /// <summary>
        /// Creates a dictionary of the offline layers available at the file path
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns>Dictionary of offline layers</returns>
        public static Dictionary<string, ILayer> GetOfflineLayers(string dirPath)
        {
            //Create a dictionary of all the offline files available
            var offlineLayers = new Dictionary<string, ILayer>();
            foreach (var file in System.IO.Directory.GetFiles(dirPath))
            {
                if (file.EndsWith(".mbtiles"))
                {
                    try
                    {
                        var offlineLayer = WMSLayer.CreateOfflineLayer(file);
                        if (!offlineLayers.Keys.Contains(offlineLayer.Name) && offlineLayer.Name != "swisstopo pixelkarte" && offlineLayer.Name != "swissimage" && offlineLayer.Name != "osm")
                        {
                            var info = new FileInfo(file);
                            offlineLayer.Tag = info.Name.Remove(info.Name.Length - 8);
                            offlineLayers.Add(offlineLayer.Name, offlineLayer);
                        }
                    }
                    catch
                    {

                    }
                }
            }
            return offlineLayers;
        }

        /// <summary>
        /// Converts the numeric storage size to a string in the appropriate units
        /// </summary>
        /// <param name="size"></param>
        /// <returns>A string reflecting the storage size</returns>
        public static string GetStorageStringForSize(double size)
        {
            if (size < 1000)
            { return string.Format("{0:N2}", size).ToString() + " bytes"; }
            else if (size < (1000 * 1000))
            { return string.Format("{0:N2}", (size / 1000)).ToString() + " kB"; }
            else if (size < (1000 * 1000 * 1000))
            { return string.Format("{0:N2}", (size / (1000 * 1000))).ToString() + " MB"; }
            else
            { return string.Format("{0:N2}", (size / (1000 * 1000 * 1000))).ToString() + " GB"; }
        }

        /// <summary>
        /// Looks for the mbtiles file associated with the layer, calculates the size of the file and returns a string reflecting the size of the stored data associated with the layer
        /// </summary>
        /// <param name="layerName"></param>
        /// <returns>A string reflecting the stored file size</returns>
        public static string GetLocalStorageSizeForLayer(string layerName)
        {
            var dirPath = App.TileLocation;
            var path = dirPath + "/" + layerName;
            FileInfo fi = new FileInfo(dirPath + "/" + layerName + ".mbtiles");
            long storage = 0;
            if (fi != null)
            {
                try
                {
                    storage = fi.Length;
                    return MapModel.GetStorageStringForSize(storage);
                }
                catch
                {
                    return "0 MB";
                }
            }
            return "0 MB";
        }

        /// <summary>
        /// Deletes the locally stored mbtiles file associated with the layer
        /// </summary>
        /// <param name="layername"></param>
        public async static Task<bool> DeleteMapLayer(string layername)
        {
            var dirPath = App.TileLocation;

            //Create a dictionary of all the offline files available
            var offlineLayers = new Dictionary<string, ILayer>();
            foreach (var file in System.IO.Directory.GetFiles(dirPath))
            {
                if (file.EndsWith(".mbtiles"))
                {
                    var filePath = dirPath + "/" + layername + ".mbtiles";
                    if (file == filePath)
                    {
                        File.Delete(filePath);
                        break;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Returns a layer for the basemap according to that chosen by the user and saved in settings. If offline and an associated mbtiles file is present, the mbtiles layer is returned.
        /// </summary>
        /// <returns></returns>
        public static MapLayer GetBaseMap()
        {
            var BL = Preferences.Get("BaseLayer", "swisstopo_pixelkarte");

            bool noInternet = MapModel.IsAppDisconnected();
            

            var wmtsLayer = WMSLayer.CreatePixelkarteWMTSTileLayer();

            if (BL == "osm")
            {
                wmtsLayer = OpenStreetMap.CreateTileLayer();
            }
            else if (BL == "swissimage")
            {
                wmtsLayer = WMSLayer.CreateSwissimageWMTSTileLayer();
            }
            var baseLayer = new MapLayer(-999, true, 0, wmtsLayer);

            if (noInternet)
            {
                var filename = "swisstopo_pixelkarte.mbtiles";
                if (BL == "osm") { filename = "osm.mbtiles"; }
                else if (BL == "swissimage") { filename = "swissimage.mbtiles"; }
                foreach (var file in System.IO.Directory.GetFiles(App.TileLocation))
                {
                    if (file.EndsWith(filename))
                    {
                        var offlineLayer = WMSLayer.CreateOfflineLayer(file) as TileLayer;
                        baseLayer = new MapLayer(-999, true, 0, offlineLayer);
                        return baseLayer;
                    }
                }
            }

            return baseLayer;

        }


        /// <summary>
        /// Create a point geometry layer for all point geometries
        /// </summary>
        /// <param name="points"></param>
        /// <returns>layer</returns>
        private static ILayer CreatePointLayer(List<Feature> points)
        {
            return new Mapsui.Layers.Layer("Points")
            {
                CRS = "EPSG:3857",
                DataSource = new MemoryProvider(points),
                IsMapInfoLayer = true,
                Style = CreateSavedBitmapStyle()
            };
        }

        /// <summary>
        /// Create a point geometry layer for all point geometries that have no associated records
        /// </summary>
        /// <param name="points"></param>
        /// <returns>layer</returns>
        private static ILayer CreatePointLayerNoRecords(List<Feature> points)
        {
            return new Mapsui.Layers.Layer("Points")
            {
                CRS = "EPSG:3857",
                DataSource = new MemoryProvider(points),
                IsMapInfoLayer = true,
                Style = CreateSavedBitmapStyleNoRecords()
            };
        }

        /// <summary>
        /// Create a layer for a temporary point (the green point created when defining a point)
        /// </summary>
        /// <param name="points"></param>
        /// <returns>layer</returns>
        private static ILayer CreateTempPointLayer(List<Feature> points)
        {
            return new Mapsui.Layers.Layer("Points")
            {
                CRS = "EPSG:3857",
                DataSource = new MemoryProvider(points),
                IsMapInfoLayer = true,
                Style = CreateBitmapStyle()
            };
        }

        /// <summary>
        /// Read the bitmap for a temporary (green) point
        /// </summary>
        /// <returns>The bitmap as a style object</returns>
        private static SymbolStyle CreateBitmapStyle()
        {
            var path = "BioDivCollectorXamarin.Images.loc.png";
            var bitmapId = GetBitmapIdForEmbeddedResource(path);
            return new SymbolStyle { BitmapId = bitmapId, SymbolScale = 0.40, SymbolOffset = new Offset(0, 0) };
        }

        /// <summary>
        /// Read the bitmap for a saved (blue) point
        /// </summary>
        /// <returns>The bitmap as a style object</returns>
        private static SymbolStyle CreateSavedBitmapStyle()
        {
            var path = "BioDivCollectorXamarin.Images.locSaved.png";
            var bitmapId = GetBitmapIdForEmbeddedResource(path);
            return new SymbolStyle { BitmapId = bitmapId, SymbolScale = 0.40, SymbolOffset = new Offset(0, 0) };
        }

        /// <summary>
        /// Read the bitmap for a saved point that has no records (orange)
        /// </summary>
        /// <returns>The bitmap as a style object</returns>
        private static SymbolStyle CreateSavedBitmapStyleNoRecords()
        {
            var path = "BioDivCollectorXamarin.Images.locNoEntry.png";
            var bitmapId = GetBitmapIdForEmbeddedResource(path);
            return new SymbolStyle { BitmapId = bitmapId, SymbolScale = 0.40, SymbolOffset = new Offset(0, 0) };
        }

        /// <summary>
        /// Get the id of the embedded bitmap
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns>Embedded resource id</returns>
        public static int GetBitmapIdForEmbeddedResource(string imagePath)
        {
            var assembly = typeof(MapPageVM).GetTypeInfo().Assembly;
            var image = assembly.GetManifestResourceStream(imagePath);
            return BitmapRegistry.Instance.Register(image);
        }

        /// <summary>
        /// Create a layer for all polygon geometries
        /// </summary>
        /// <param name="polygons"></param>
        /// <param name="lineColour"></param>
        /// <param name="fillColour"></param>
        /// <returns>The polygon layer</returns>
        public static ILayer CreatePolygonLayer(List<Feature> polygons, Mapsui.Styles.Color lineColour, Mapsui.Styles.Color fillColour)
        {
            return new Mapsui.Layers.Layer("Polygons")
            {
                CRS = "EPSG:3857",
                DataSource = new MemoryProvider(polygons),
                IsMapInfoLayer = true,
                Style = new VectorStyle
                {
                    Fill = new Mapsui.Styles.Brush(fillColour),
                    Outline = new Pen
                    {
                        Color = lineColour,
                        Width = 5,
                        PenStyle = PenStyle.Solid,
                        PenStrokeCap = PenStrokeCap.Round
                    }
                }
            };
        }

        /// <summary>
        /// Create a layer for all line geometries
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="colour"></param>
        /// <returns>The line layer</returns>
        public static ILayer CreateLineLayer(List<Feature> lines, Mapsui.Styles.Color colour)
        {
            return new Mapsui.Layers.Layer("Lines")
            {
                CRS = "EPSG:3857",
                DataSource = new MemoryProvider(lines),
                IsMapInfoLayer = true,
                Style = new VectorStyle
                {
                    Fill = null,
                    Outline = null,
                    Line =
                    {
                        Color = colour,
                        Width = 5
                    }
                }
            };
        }

        /// <summary>
        /// Find the centroid of the geometry
        /// </summary>
        /// <param name="geometryId"></param>
        /// <returns>A point object representing the centroid</returns>
        public static async Task<Mapsui.Geometries.Point> GetCentreOfGeometry(int geometryId)
        {
            var items = await DataDAO.getDataForMap(App.CurrentProjectId);
            foreach (var item in items)
            {
                if (item.geomId == geometryId)
                {
                    var coords = item.shapeGeom.Centroid;
                    var centre = SphericalMercator.FromLonLat(coords.X, coords.Y);
                    return centre;
                }
            }
            return null;
        }

        /// <summary>
        /// Create each of the geometries, style them and add them to both the respective geometry type array and the 'AllShapes'array (used for centering the map)
        /// </summary>
        /// <returns>A dictionary of geometry layers</returns>
        public static async Task<Dictionary<string, ILayer>> CreateShapes()
        {
            var layerDic = new Dictionary<string, ILayer>();
            var items = await DataDAO.getDataForMap(App.CurrentProjectId);


            if (items != null && items.Count > 0)
            {
                var points = new List<Feature>();
                var polygons = new List<Feature>();
                var lines = new List<Feature>();
                var pointsNoRecords = new List<Feature>();
                var polygonsNoRecords = new List<Feature>();
                var linesNoRecords = new List<Feature>();
                var allShapes = new List<Feature>();

                foreach (Shape item in items)
                {
                    var coords = item.shapeGeom.Coordinates;
                    var coordCount = item.shapeGeom.Coordinates.Length;
                    var hasRecords = false;
                        //hasRecords = conn.Table<Record>().Select(c => c).Where(c => c.geometry_fk == item.geomId).Count() > 0;
                        var recordList = await Record.FetchRecordByGeomId(item.geomId);
                        hasRecords = recordList.Count > 0;
                    if (coordCount > 0)
                    {
                        if (coordCount == 1)
                        {
                            //Point
                            var coord = coords[0];

                            var point = SphericalMercator.FromLonLat(coord.X, coord.Y);

                            var feature = new Feature
                            {
                                Geometry = point,
                                ["Name"] = item.geomId.ToString(),
                                ["Label"] = item.title
                            };
                            feature.Styles.Add(new LabelStyle
                            {
                                Text = item.title,
                                BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.White),
                                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                                Offset = new Offset(20, 0, false)
                            });
                            if (hasRecords) { points.Add(feature); } else {
                                pointsNoRecords.Add(feature); }
                            allShapes.Add(feature);
                        }
                        else
                        {
                            var coord0 = coords[0];
                            var coordx = coords[coordCount - 1];
                            if (coord0.X == coordx.X && coord0.Y == coordx.Y)
                            {
                                //Polygon
                                var polygon = new Polygon();

                                var localCoords = coords;
                                foreach (NetTopologySuite.Geometries.Coordinate coord in localCoords)
                                {
                                    var pt = SphericalMercator.FromLonLat(coord.X, coord.Y);
                                    polygon.ExteriorRing.Vertices.Add(new Mapsui.Geometries.Point(pt.X, pt.Y));

                                }
                                var feature = new Feature
                                {
                                    Geometry = polygon,
                                    ["Name"] = item.geomId.ToString(),
                                    ["Label"] = item.title
                                };
                                feature.Styles.Add(new LabelStyle
                                {
                                    Text = item.title,
                                    BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.White),
                                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center
                                });

                                if (hasRecords) { polygons.Add(feature); } else { polygonsNoRecords.Add(feature); }
                                allShapes.Add(feature);
                            }
                            else
                            {
                                //Line
                                var line = new LineString();
                                var localCoords = coords;
                                foreach (NetTopologySuite.Geometries.Coordinate coord in localCoords)
                                {
                                    var pt = SphericalMercator.FromLonLat(coord.X, coord.Y);
                                    line.Vertices.Add(new Mapsui.Geometries.Point(pt.X, pt.Y));
                                }
                                var feature = new Feature
                                {
                                    Geometry = line,
                                    ["Name"] = item.geomId.ToString(),
                                    ["Label"] = item.title
                                };
                                feature.Styles.Add(new LabelStyle
                                {
                                    Text = item.title,
                                    BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.White),
                                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center
                                });
                                if (hasRecords) { lines.Add(feature); } else { linesNoRecords.Add(feature); }
                                allShapes.Add(feature);
                            }
                        }
                    }
                }

                ILayer polygonLayer = CreatePolygonLayer(polygons, new Mapsui.Styles.Color(0, 0, 200, 255), new Mapsui.Styles.Color(0, 0, 200, 32));
                ILayer pointLayer = CreatePointLayer(points);
                ILayer lineLayer = CreateLineLayer(lines, new Mapsui.Styles.Color(0, 0, 200, 255));
                ILayer polygonLayerNoRecords = CreatePolygonLayer(polygonsNoRecords, new Mapsui.Styles.Color(255, 166, 0, 255), new Mapsui.Styles.Color(255, 166, 0, 32));
                ILayer pointLayerNoRecords = CreatePointLayerNoRecords(pointsNoRecords);
                ILayer lineLayerNoRecords = CreateLineLayer(linesNoRecords, new Mapsui.Styles.Color(255, 166, 0, 255));
                ILayer allShapesLayer = CreatePolygonLayer(allShapes, new Mapsui.Styles.Color(0, 0, 200, 255), new Mapsui.Styles.Color(0, 0, 200, 32)); //AllShapes layer is created merely to get bounding box of all shapes. It does not have the correct styles for showing all the shapes


                layerDic.Add("polygons", polygonLayer);
                layerDic.Add("lines", lineLayer);
                layerDic.Add("points", pointLayer);
                layerDic.Add("polygonsNoRecords", polygonLayerNoRecords);
                layerDic.Add("linesNoRecords", lineLayerNoRecords);
                layerDic.Add("pointsNoRecords", pointLayerNoRecords);
                layerDic.Add("all", allShapesLayer);

            }
            return layerDic;
        }

        /// <summary>
        /// Check that a polygon is valid. It must have 3 or more points, and the lines must not intersect
        /// </summary>
        /// <param name="pointList"></param>
        /// <returns>Valid/not valid</returns>
        public static bool CheckValidityOfPolygon(List<Mapsui.Geometries.Point> pointList)
        {
            if (pointList.Count() < 4) { return false; }
            var polygon = new Polygon();

            foreach (var coord in pointList)
            {
                polygon.ExteriorRing.Vertices.Add(new Mapsui.Geometries.Point(coord.X, coord.Y));
            }
            var wkt = Mapsui.Geometries.WellKnownText.GeometryToWKT.Write(polygon);
            WKTReader reader = new WKTReader();
            NetTopologySuite.Geometries.Geometry geom = reader.Read(wkt);
            return geom.IsValid;
        }

        /// <summary>
        /// Create a layer for an unsaved (green) geometry. Used when defining the geometry
        /// </summary>
        /// <param name="pointList"></param>
        /// <returns>Temporary geometry layer</returns>
        public static ILayer CreateTempLayer(List<Mapsui.Geometries.Point> pointList)
        {
            var layerDic = new Dictionary<string, ILayer>();


            var points = new List<Feature>();
            var polygons = new List<Feature>();
            var lines = new List<Feature>();
            var allShapes = new List<Feature>();

            var coords = pointList;
            var coordCount = pointList.Count;
            if (coordCount > 0)
            {
                if (coordCount == 1)
                {
                    //Point
                    var coord = coords[0];

                    var point = SphericalMercator.FromLonLat(coord.X, coord.Y);

                    var feature = new Feature
                    {
                        Geometry = point,
                        ["Name"] = Guid.NewGuid().ToString(),
                        ["Label"] = ""
                    };
                    feature.Styles.Add(new LabelStyle
                    {
                        Text = "",
                        BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.White),
                        HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                        Offset = new Offset(20, 0, false)
                    });
                    points.Add(feature);
                    ILayer pointLayer = MapModel.CreateTempPointLayer(points);
                    pointLayer.IsMapInfoLayer = false; //Ensure that the app doesn't try to select the temp layer when clicking on it
                    return pointLayer;
                }
                else
                {
                    var coord0 = coords[0];
                    var coordx = coords[coordCount - 1];
                    if (coord0.X == coordx.X && coord0.Y == coordx.Y)
                    {
                        //Polygon
                        var polygon = new Polygon();

                        var localCoords = coords;
                        foreach (var coord in pointList)
                        {
                            var pt = SphericalMercator.FromLonLat(coord.X, coord.Y);
                            polygon.ExteriorRing.Vertices.Add(new Mapsui.Geometries.Point(pt.X, pt.Y));

                        }
                        var feature = new Feature
                        {
                            Geometry = polygon,
                            ["Name"] = Guid.NewGuid().ToString(),
                            ["Label"] = ""
                        };
                        feature.Styles.Add(new LabelStyle
                        {
                            Text = "",
                            BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.White),
                            HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center
                        });

                        polygons.Add(feature);
                        ILayer polygonLayer = MapModel.CreatePolygonLayer(polygons, new Mapsui.Styles.Color(59, 181, 11, 255), new Mapsui.Styles.Color(59, 181, 11, 32));
                        polygonLayer.IsMapInfoLayer = false;  //Ensure that the app doesn't try to select the temp layer when clicking on it
                        return polygonLayer;
                    }
                    else
                    {
                        //Line
                        var line = new LineString();
                        var localCoords = coords;
                        foreach (var coord in localCoords)
                        {
                            var pt = SphericalMercator.FromLonLat(coord.X, coord.Y);
                            line.Vertices.Add(new Mapsui.Geometries.Point(pt.X, pt.Y));
                        }
                        var feature = new Feature
                        {
                            Geometry = line,
                            ["Name"] = Guid.NewGuid().ToString(),
                            ["Label"] = ""
                        };
                        feature.Styles.Add(new LabelStyle
                        {
                            Text = "",
                            BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.White),
                            HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center
                        });

                        lines.Add(feature);
                        ILayer lineLayer = MapModel.CreateLineLayer(lines, new Mapsui.Styles.Color(59, 181, 11, 255));
                        lineLayer.IsMapInfoLayer = false;  //Ensure that the app doesn't try to select the temp layer when clicking on it
                        return lineLayer;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Save each of the map layers on-screen (the extent is provided by the screen edges) to an mbtiles file
        /// </summary>
        /// <param name="extent"></param>
        public static async Task saveMaps(Extent extent)
        {
            
            var basemap = Preferences.Get("BaseLayer", "swisstopo_pixelkarte"); //Check which basemap
            var cancel = false;
            MessagingCenter.Subscribe<Application>(App.Current, "CancelMapSave", (sender) =>
            {
                //Listen for tile save updates and update the count
                cancel = true;
            });


            //Get the online layers
            var layers = await GetLayersForMap(App.CurrentProjectId);
            //Create an array for adding the layers into in an ordered fashion
            var mapLayersTemp = new MapLayer[layers.Count];
            //Add online wms layers
            var layerStack = layers.OrderBy(o => o.order).ToList();
            //Calculate number of tiles
            var noOfLayers = 0;

            //Calculate number of tiles
            long noOfTiles = 0;
            foreach (var layer in layerStack)
            {
                if (layer.visible)
                {
                    var tileSource = WMSLayer.CreateTileSource(layer.url, layer.wmsLayer, "EPSG:3857");

                    foreach (var zoomScale in tileSource.Schema.Resolutions)
                    {
                        var tileInfos = tileSource.Schema.GetTileInfos(extent, zoomScale.Value.UnitsPerPixel);
                        noOfTiles = noOfTiles + tileInfos.Count();
                    }
                    noOfLayers++;
                }

            }

            if (basemap == "swisstopo_pixelkarte")
            {
                string swisstopoDbpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "mbtiles/swisstopo.mbtiles");
                TileLayer swisstopo = WMSLayer.CreatePixelkarteWMTSTileLayer();
                foreach (var zoomScale in swisstopo.TileSource.Schema.Resolutions)
                {
                    var stTileInfos = swisstopo.TileSource.Schema.GetTileInfos(extent, zoomScale.Value.UnitsPerPixel);
                    noOfTiles = noOfTiles + stTileInfos.Count();
                }
            }
            else if (basemap == "swissimage")
            {
                string swisstopoDbpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "mbtiles/swissimage.mbtiles");
                TileLayer swisstopo = WMSLayer.CreateSwissimageWMTSTileLayer();
                foreach (var zoomScale in swisstopo.TileSource.Schema.Resolutions)
                {
                    var stTileInfos = swisstopo.TileSource.Schema.GetTileInfos(extent, zoomScale.Value.UnitsPerPixel);
                    noOfTiles = noOfTiles + stTileInfos.Count();
                }
            }
            else
            {
                string osmDbpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "mbtiles/osm.mbtiles");
                TileLayer osm = OpenStreetMap.CreateTileLayer();
                foreach (var zoomScale in osm.TileSource.Schema.Resolutions)
                {
                    var osmTileInfos = osm.TileSource.Schema.GetTileInfos(extent, zoomScale.Value.UnitsPerPixel);
                    noOfTiles = noOfTiles + osmTileInfos.Count();
                }
            }


            Console.WriteLine(noOfTiles + " tiles in " + (noOfLayers + 1) + " layers");


            //Save tiles
            long tilesSaved = 0;

            MessagingCenter.Subscribe<Application>(App.Current, "TileSavedInternal", async (sender) =>
            {
                //Listen for tile save updates and update the count

                var message = tilesSaved + " von " + noOfTiles + " Kacheln aus " + (noOfLayers + 1) + " Ebenen gespeichert";
                if (noOfLayers == 0)
                { message = tilesSaved + " von " + noOfTiles + " Kacheln aus " + (noOfLayers + 1) + " Ebene gespeichert"; }

                Console.WriteLine(message);
                MessagingCenter.Send<Application, string>(App.Current, "TileSaved", message);
                if (tilesSaved >= noOfTiles - 5 || cancel)
                {
                    MessagingCenter.Send<Application, string>(App.Current, "TileSaved", String.Empty);
                    MessagingCenter.Unsubscribe<Application>(App.Current, "TileSavedInternal");
                    await MapModel.MakeArrayOfLayers();
                }
            });

            var tasks = new List<Task>(); // Each layer has to be saved in a single thread due to database access, but we can save layers concurrently

            foreach (var layer in layerStack)
            {
                if (cancel) { break; }
                if (layer.visible)
                {
                    var layerNo = Math.Max(layers.Count - layer.order, 0);
                    var tileSource = WMSLayer.CreateTileSource(layer.url, layer.wmsLayer, "EPSG:3857");
                    string dbpath = Path.Combine(DependencyService.Get<FileInterface>().GetMbTilesPath(), layer.title + ".mbtiles");
                    //extent = tileSource.Schema.Extent;

                    var layerTask =  Task.Run(async () =>
                    {
                            //Create the database
                            Mbtiles.CreateMbTiles(dbpath, layer, tileSource);

                        //Populate the database
                        foreach (var zoomScale in tileSource.Schema.Resolutions)
                        {
                            if (cancel) { break; }
                            await Task.Run(async() =>
                           {
                               var tileInfos = tileSource.Schema.GetTileInfos(extent, zoomScale.Value.UnitsPerPixel);

                               foreach (var tileInfo in tileInfos)
                               {
                                   if (cancel) { break; }
                                   await Task.Run(() =>
                                  {
                                      tilesSaved++;
                                      saveTile(tileSource, tileInfo, dbpath, layer);
                                  });
                               }
                           });
                        }
                        Console.WriteLine("Saving of 1 layer complete");
                    });
                    tasks.Add(layerTask);
                }
           
            }

            //Swisstopo layer
            var task = Task.Run(async () =>
            {
                string baselayerDbsavepath = Path.Combine(DependencyService.Get<FileInterface>().GetMbTilesPath(), "swisstopo_pixelkarte.mbtiles");
                TileLayer baselayer = WMSLayer.CreatePixelkarteWMTSTileLayer();

                //Create the database
                var baseLayer = new BioDivCollectorXamarin.Models.DatabaseModel.Layer();
                baseLayer.title = "swisstopo pixelkarte";

                if (basemap == "osm") //OSM case
                {
                    baselayerDbsavepath = Path.Combine(DependencyService.Get<FileInterface>().GetMbTilesPath(), "osm.mbtiles");
                    baselayer = OpenStreetMap.CreateTileLayer();

                    //Create the database
                    baseLayer = new BioDivCollectorXamarin.Models.DatabaseModel.Layer();
                    baseLayer.title = "osm";
                }
                else if (basemap == "swissimage") //Swissimage case
                {
                    baselayerDbsavepath = Path.Combine(DependencyService.Get<FileInterface>().GetMbTilesPath(), "swissimage.mbtiles");
                    baselayer = WMSLayer.CreateSwissimageWMTSTileLayer();

                    //Create the database
                    baseLayer = new BioDivCollectorXamarin.Models.DatabaseModel.Layer();
                    baseLayer.title = "swissimage";
                }
                Mbtiles.CreateMbTiles(baselayerDbsavepath, baseLayer, baselayer.TileSource);

                //Populate the database
                foreach (var zoomScale in baselayer.TileSource.Schema.Resolutions)
                {
                    if (cancel) { break; }
                    await Task.Run(async () =>
                    {
                        var baseLayerTileInfos = baselayer.TileSource.Schema.GetTileInfos(extent, zoomScale.Value.UnitsPerPixel);
                        foreach (var tileInfo in baseLayerTileInfos)
                        {

                            if (cancel) { break; }
                            await Task.Run(() =>
                            {
                                tilesSaved++;
                                saveTile(baselayer.TileSource, tileInfo, baselayerDbsavepath, baseLayer);
                            });
                        }
                    });
                    
                }
                Console.WriteLine("Baselayer saved");
            });
            tasks.Add(task);
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Save a given map tile to an Mbtiles file
        /// </summary>
        /// <param name="tileSource"></param>
        /// <param name="tileInfo"></param>
        /// <param name="dbpath"></param>
        /// <param name="layer"></param>
        public static void saveTile(ITileSource tileSource, TileInfo tileInfo, string dbpath, BioDivCollectorXamarin.Models.DatabaseModel.Layer layer)
        {
            if (!Mbtiles.TileExists(tileInfo, dbpath))
            {
                try
                {
                    var tile = tileSource.GetTile(tileInfo);
                    Mbtiles.PopulateMbtilesWith(tile, tileInfo, dbpath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    MessagingCenter.Send<Application>(App.Current, "TileSavedInternal");
                }
            }
            else
            {
                MessagingCenter.Send<Application>(App.Current, "TileSavedInternal");
            }
            

        }

        /// <summary>
        /// Check if the app is connected (online)
        /// </summary>
        /// <returns>Whether the app is disconnected</returns>
        public static bool IsAppDisconnected()
        {
            if (Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        ///  Add layers to the database corresponding to mbtiles files
        /// </summary>
        public static async Task AddOfflineLayersToProject()
        {
            var dirPath = App.TileLocation;
            //Get the offline layers
            var offlineLayers = GetOfflineLayers(dirPath);
            //Get the online layers
            var layers = await GetLayersForMap(App.CurrentProjectId);
            //Add online wms layers
            var layerStack = layers.OrderBy(o => o.order).ToList();

            int i = 1;
            //Identify the offline only layers
            foreach (var layertitle in offlineLayers.Keys)
            {
                var online = false;
                foreach (var onlinelayer in layers)
                {
                    if (onlinelayer.title == layertitle)
                    {
                        online = true;
                        break;
                    }
                }
                if (online == false)
                {
                    offlineLayers.TryGetValue(layertitle, out ILayer layer);
                    var lay = new DatabaseModel.Layer();
                    var path = dirPath + "/" + layertitle;
                    var proj = Project.FetchProject(App.CurrentProjectId);
                    await BioDivCollectorXamarin.Models.DatabaseModel.Layer.AddFileLayer(layer.Name, layertitle, dirPath + "/" + layertitle + ".mbtiles", proj.Id, layers.Count + i++);
                }
            }
        }

        public static async Task RemoveOfflineLayersFromProject()
        {
            var proj = Project.FetchProject(App.CurrentProjectId);
            await DatabaseModel.Layer.RemoveFileLayers(proj.Id);
        }
    }
}
