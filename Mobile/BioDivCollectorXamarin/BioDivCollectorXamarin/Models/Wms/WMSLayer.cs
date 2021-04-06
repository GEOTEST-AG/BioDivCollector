using BruTile;
using BruTile.Predefined;
using BruTile.Web;
using BruTile.Wmsc;
using Mapsui.Layers;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BruTile.MbTiles;

namespace BioDivCollectorXamarin.Models.Wms
{
    class WMSLayer
    {
        //Tiled WMS
        public static ILayer CreateWMSLayer(string urlString, string layerName, string CRS, string layerTitle)
        {
            return new TileLayer(CreateTileSource(urlString, layerName, CRS));
        }

        public static ITileSource CreateTileSource(string urlString, string layerName, string CRS)
        {
            var schema = new GlobalSphericalMercator("png", YAxis.OSM, 0, 21, null);
            var layers = new List<string>();
            layers.Add(layerName);
            var styles = new List<string>();
            if (!urlString.ToLower().Contains("&version="))
            {
                urlString = urlString + "&version=1.3.0";
            }
            if (!urlString.ToLower().Contains("&crs="))
            {
                urlString = urlString + "&CRS=" + CRS;
            }
            if (!urlString.ToLower().Contains("&format="))
            {
                urlString = urlString + "&format=png";
            }
            if (!urlString.ToLower().Contains("&transparent="))
            {
                urlString = urlString + "&transparent=true";
            }
            var request = new WmscRequest(new Uri(urlString), schema, layers, new string[0].ToList());
            var provider = new HttpTileProvider(request);
            return new TileSource(provider, schema);
        }


        //Full Image WMS
        public static ILayer CreateWMSImageLayer(string urlString, string layerName, string CRS, string layerTitle)
        {
            return new ImageLayer(layerTitle) { DataSource = CreateWmsImageProvider(urlString, layerName, CRS) };
        }

        private static WmsProvider CreateWmsImageProvider(string urlString, string layerName, string CRS)
        {
            var provider = new WmsProvider(urlString)
            {
                ContinueOnError = true,
                TimeOut = 20000,
                CRS = CRS
            };

            provider.AddLayer(layerName);
            provider.SetImageFormat(provider.OutputFormats[0]);
            return provider;
        }


        //WMTS
        private static readonly BruTile.Attribution OpenStreetMapAttribution = new BruTile.Attribution("© Swisstopo", "https://www.swisstopo.admin.ch/en/home/meta/conditions/geodata/ogd.html");

        //Swisstopo Pixelkarte
        public static TileLayer CreatePixelkarteWMTSTileLayer()
        {
            return new TileLayer(CreatePixelkarteWMTSTileSource()) { Name = "swisstopo-pixelkarte" };
        }

        private static HttpTileSource CreatePixelkarteWMTSTileSource()
        {
            return new HttpTileSource(new GlobalSphericalMercator(0, 21),
                "https://wmts.geo.admin.ch/1.0.0/ch.swisstopo.pixelkarte-farbe/default/current/3857/{z}/{x}/{y}.jpeg",
                null, name: "swisstopo",
                attribution: OpenStreetMapAttribution, userAgent: "Swisstopo-pixelkarte in Mapsui");
        }

        //Swisstopo Pixelkarte WMS
        public static ILayer CreatePixelkarteWMSLayer()
        {
            return new TileLayer(CreateTileSource("https://wms.geo.admin.ch/?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities", "ch.swisstopo.pixelkarte-farbe", "EPSG:3857"));
        }

        //Swissimage
        public static TileLayer CreateSwissimageWMTSTileLayer()
        {
            return new TileLayer(CreateSwissimageWMTSTileSource()) { Name = "swissimage" };
        }

        private static HttpTileSource CreateSwissimageWMTSTileSource()
        {
            return new HttpTileSource(new GlobalSphericalMercator(0, 21),
                "https://wmts.geo.admin.ch/1.0.0/ch.swisstopo.swissimage/default/current/3857/{z}/{x}/{y}.jpeg",
                null, name: "swissimage",
                attribution: OpenStreetMapAttribution, userAgent: "Swissimage in Mapsui");
        }

        //Swissimage WMS
        public static ILayer CreateSwissimageWMSLayer()
        {
            return new TileLayer(CreateTileSource("https://wms.geo.admin.ch/?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities", "ch.swisstopo.swissimage", "EPSG:3857"));
        }

        //MBTiles 
        public static ILayer CreateOfflineLayer(string filePath)
        {
            try
            {
                ITileSource tileSource = CreateOfflineSource(filePath);
                return new TileLayer(tileSource) { Name = tileSource.Name, CRS = "EPSG:3857" };
            }
            catch
            {
                return null;
            }
        }

        public static ITileSource CreateOfflineSource(string filePath)
        {
            try
            {
                ITileSource layerSource = new MbTilesTileSource(new SQLiteConnectionString(filePath, true));
                return layerSource;
            }
            catch
            {
                return null;
            }
            
        }

    }
}