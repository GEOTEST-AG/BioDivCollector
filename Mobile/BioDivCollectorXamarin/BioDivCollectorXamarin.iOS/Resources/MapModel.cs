using BioDivCollectorXamarin.Models.Wms;
using BioDivCollectorXamarin.ViewModels;
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

namespace BioDivCollectorXamarin.Models
{
    class MapModel
    {
        //Mapserver layers

        public ObservableCollection<MapLayer> MakeArrayOfLayers()
        {
			int i = 0;
            //Add mbtiles layers
            var dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "mbtiles");
            //Create directory if it doesn't exist
            if (!File.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            //Create a dictionary of all the offline files available
            var offlineLayers = new Dictionary<string, ILayer>();
            foreach (var file in System.IO.Directory.GetFiles(dirPath))
            {
                if (file.EndsWith(".mbtiles"))
                {
                    var offlineLayer = WMSLayer.CreateOfflineLayer(file);
                    offlineLayers.Add(offlineLayer.Name, offlineLayer);
                }
            }

            //Get the online layers
            var layers = GetLayersForMap(App.CurrentProjectId);
            //Create an array for adding the layers into in an ordered fashion
            var mapLayersTemp = new MapLayer[layers.Count];
            //Add online wms layers
            var layerStack = layers.OrderBy(o => o.order).ToList();
            
            foreach (var layer in layerStack)
            {
                //Now add the layers in their correct order
                try
                {
                    var layerNo = Math.Max(layers.Count - layer.order, 0);
                    var layerWms = WMSLayer.CreateWMSLayer(layer.url, layer.wmsLayer, "EPSG:3857", layer.title);
                    layerWms.Opacity = layer.opacity;
                    layerWms.Enabled = layer.visible;

                    if (Connectivity.NetworkAccess != NetworkAccess.Internet && offlineLayers.Keys.Contains(layerWms.Name))
                    {
                        //If no internet, check for saved tiles
                        ILayer offlineLayer;
                        offlineLayers.TryGetValue(layerWms.Name, out offlineLayer);
                        if (offlineLayer != null)
                        {
                            var WmsLayer = new MapLayer(true, 0, offlineLayer);
                            WmsLayer.Opacity = layerWms.Opacity;
                            WmsLayer.Enabled = layerWms.Enabled;
                            WmsLayer.LayerZ = layer.order;
                            WmsLayer.Name = layer.title;
                            mapLayersTemp.SetValue(WmsLayer, layerNo);
                            i++;
                        }
                    }
                    else
                    {
                        //If internet, read directly from WMS
                        if (layerWms != null)
                        {
                            var WmsLayer = new MapLayer(true, 0, layerWms);
                            WmsLayer.Opacity = layerWms.Opacity;
                            WmsLayer.Enabled = layerWms.Enabled;
                            WmsLayer.LayerZ = layer.order;
                            WmsLayer.Name = layer.title;
                            mapLayersTemp.SetValue(WmsLayer, layerNo);
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

            return new ObservableCollection<MapLayer>(mapLayersTempList as List<MapLayer>);

        }



        //BioDiv Layers
        private static ILayer CreatePointLayer(List<Feature> points)
        {
            return new Layer("Points")
            {
                CRS = "EPSG:3857",
                DataSource = new MemoryProvider(points),
                IsMapInfoLayer = true,
                Style = CreateSavedBitmapStyle()
            };
        }

        private static ILayer CreateTempPointLayer(List<Feature> points)
        {
            return new Layer("Points")
            {
                CRS = "EPSG:3857",
                DataSource = new MemoryProvider(points),
                IsMapInfoLayer = true,
                Style = CreateBitmapStyle()
            };
        }


        private static SymbolStyle CreateBitmapStyle()
        {
            var path = "BioDivCollectorXamarin.Images.loc.png"; 
            var bitmapId = GetBitmapIdForEmbeddedResource(path);
            var bitmapHeight = 176; // To set the offset correct we need to know the bitmap height
            return new SymbolStyle { BitmapId = bitmapId, SymbolScale = 0.40, SymbolOffset = new Offset(0, 0) };
        }

        private static SymbolStyle CreateSavedBitmapStyle()
        {
            var path = "BioDivCollectorXamarin.Images.locSaved.png";
            var bitmapId = GetBitmapIdForEmbeddedResource(path);
            var bitmapHeight = 176; // To set the offset correct we need to know the bitmap height
            return new SymbolStyle { BitmapId = bitmapId, SymbolScale = 0.40, SymbolOffset = new Offset(0, 0) };
        }

        private static int GetBitmapIdForEmbeddedResource(string imagePath)
        {
            var assembly = typeof(MapPageVM).GetTypeInfo().Assembly;
            var image = assembly.GetManifestResourceStream(imagePath);
            return BitmapRegistry.Instance.Register(image);
        }

        public static ILayer CreatePolygonLayer(List<Feature> polygons, Mapsui.Styles.Color lineColour, Mapsui.Styles.Color fillColour)
        {
            return new Layer("Polygons")
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

        public static ILayer CreateLineLayer(List<Feature> lines, Mapsui.Styles.Color colour)
        {
            return new Layer("Lines")
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


        public static Dictionary<string, ILayer> CreateShapes()
        {
            var layerDic = new Dictionary<string, ILayer>();
            var items = DataDAO.getDataForMap(App.CurrentProjectId);

            if (items != null && items.Count > 0)
            {
                var points = new List<Feature>();
                var polygons = new List<Feature>();
                var lines = new List<Feature>();
                var allShapes = new List<Feature>();

                foreach (Shape item in items)
                {
                    var coords = item.shapeGeom.Coordinates;
                    var coordCount = item.shapeGeom.Coordinates.Length;
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
                            points.Add(feature);
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
                                var feature = new Feature { 
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
                                
                                polygons.Add(feature);
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
                                lines.Add(feature);
                                allShapes.Add(feature);
                            }
                        }
                    }
                }

                ILayer polygonLayer = CreatePolygonLayer(polygons, new Mapsui.Styles.Color(0, 0, 200, 255), new Mapsui.Styles.Color(0, 0, 200, 32));
                ILayer pointLayer = CreatePointLayer(points);
                ILayer lineLayer = CreateLineLayer(lines, new Mapsui.Styles.Color(0, 0, 200, 255));
                ILayer allShapesLayer = CreatePolygonLayer(allShapes, new Mapsui.Styles.Color(0, 0, 200, 255), new Mapsui.Styles.Color(0, 0, 200, 32)); //AllShapes layer is created merely to get bounding box of all shapes. It does not have the correct styles for showing all the shapes

                
                layerDic.Add("polygons", polygonLayer);
                layerDic.Add("lines", lineLayer);
                layerDic.Add("points", pointLayer);
                layerDic.Add("all", allShapesLayer);
                
            }
            return layerDic;
        }

        public static bool CheckValidityOfPolygon(List<Mapsui.Geometries.Point> pointList)
        {
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
                        return lineLayer;
                    }
                }
            }
            return null;
        }

        public static void saveMaps (Extent extent, double resolution)
        {

            //Get the online layers
            var layers = GetLayersForMap(App.CurrentProjectId);
            //Create an array for adding the layers into in an ordered fashion
            var mapLayersTemp = new MapLayer[layers.Count];
            //Add online wms layers
            var layerStack = layers.OrderBy(o => o.order).ToList();

            foreach (var layer in layerStack)
            {

                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "");
                var files = Directory.GetFiles(filePath);
                FileStream destination = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "mbtiles/new.mbtiles"));
                using (var sourceStream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    sourceStream.CopyTo(destination);
                }

                //Now add the layers in their correct order
                var layerNo = Math.Max(layers.Count - layer.order, 0);
                var tileSource = WMSLayer.CreateTileSource(layer.url, layer.wmsLayer, "EPSG:3857");
                var tileInfos = tileSource.Schema.GetTileInfos(extent, resolution);

                // 3) Fetch the tiles from the service

                Console.WriteLine("Show tile info");
                foreach (var tileInfo in tileInfos)
                {
                    var tile = tileSource.GetTile(tileInfo);

                    Console.WriteLine(
                        $"Layer: {layer.title}, " +
                        $"tile col: {tileInfo.Index.Col}, " +
                        $"tile row: {tileInfo.Index.Row}, " +
                        $"tile level: {tileInfo.Index.Level} , " +
                        $"tile size {tile.Length}");
                }
            }
        }
    }
}
