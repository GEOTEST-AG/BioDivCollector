using BioDivCollectorXamarin.Models.DatabaseModel;
using BruTile;
using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace BioDivCollectorXamarin.Models.MBTiles
{
    /// <summary>
    /// Definition of MBTiles database structure
    /// </summary>
    [Table("map")]
    public class Map
    {
        public int zoom_level { get; set; }

        public int tile_column { get; set; }

        public int tile_row { get; set; }

        public string tile_id { get; set; }
        public string grid_id { get; set; }
    }

    [Table("grid_key")]
    public class Grid_Key
    {
        public string grid_id { get; set; }
        public string key_name { get; set; }
    }

    [Table("keymap")]
    public class Keymap
    {
        public string key_name { get; set; }
        public string key_json { get; set; }
    }

    [Table("grid_utfgrid")]
    public class Grid_UTFGrid
    {
        public string grid_id { get; set; }
        public byte[] grid_utfgrid { get; set; }
    }

    [Table("images")]
    public class Images
    {
        public byte[] tile_data { get; set; }
        public string tile_id { get; set; }
    }

    [Table("metadata")]
    public class Metadata
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    [Table("geocoder_data")]
    public class Geocoder_Data
    {
        public string type { get; set; }
        public int shard { get; set; }
        public byte[] data { get; set; }
    }

    public class Mbtiles
    {
        /// <summary>
        /// Create an mbtiles database
        /// </summary>
        /// <param name="dbname"></param>
        /// <param name="layer"></param>
        /// <param name="tileSource"></param>
        public static void CreateMbTiles(string dbname, Layer layer, ITileSource tileSource)
        {
            using (SQLiteConnection conn = new SQLiteConnection(dbname))
            {
                conn.Execute("CREATE TABLE IF NOT EXISTS map ( zoom_level INTEGER, tile_column INTEGER, tile_row INTEGER, tile_id TEXT, grid_id TEXT); ", "");
                conn.Execute("CREATE TABLE IF NOT EXISTS grid_key ( grid_id TEXT, key_name TEXT ); ", "");
                conn.Execute("CREATE TABLE IF NOT EXISTS keymap(key_name TEXT, key_json TEXT); ", "");
                conn.Execute("CREATE TABLE IF NOT EXISTS grid_utfgrid ( grid_id TEXT, grid_utfgrid BLOB ); ", "");
                conn.Execute("CREATE TABLE IF NOT EXISTS images ( tile_data blob, tile_id text ); ", "");
                conn.Execute("CREATE TABLE IF NOT EXISTS metadata ( name text, value text ); ", "");
                conn.Execute("CREATE TABLE IF NOT EXISTS geocoder_data ( type TEXT, shard INTEGER, data BLOB ); ", "");

                conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS map_index ON map (zoom_level, tile_column, tile_row); ", "");
                conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS grid_key_lookup ON grid_key (grid_id, key_name); ", "");
                conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS keymap_lookup ON keymap (key_name); ", "");
                conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS grid_utfgrid_lookup ON grid_utfgrid (grid_id); ", "");
                conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS images_id ON images (tile_id); ", "");
                conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS name ON metadata (name); ", "");
                conn.Execute("CREATE INDEX IF NOT EXISTS map_grid_id ON map (grid_id); ", "");
                conn.Execute("CREATE INDEX IF NOT EXISTS geocoder_type_index ON geocoder_data (type); ", "");
                conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS geocoder_shard_index ON geocoder_data (type, shard); ", "");

                conn.Execute("CREATE VIEW IF NOT EXISTS tiles AS SELECT map.zoom_level AS zoom_level, map.tile_column AS tile_column, map.tile_row AS tile_row, images.tile_data AS tile_data FROM map JOIN images ON images.tile_id = map.tile_id; ", "");
                conn.Execute("CREATE VIEW IF NOT EXISTS grids AS SELECT map.zoom_level AS zoom_level, map.tile_column AS tile_column, map.tile_row AS tile_row, grid_utfgrid.grid_utfgrid AS grid FROM map JOIN grid_utfgrid ON grid_utfgrid.grid_id = map.grid_id; ", "");
                conn.Execute("CREATE VIEW IF NOT EXISTS grid_data AS SELECT map.zoom_level AS zoom_level, map.tile_column AS tile_column, map.tile_row AS tile_row, keymap.key_name AS key_name, keymap.key_json AS key_json FROM map JOIN grid_key ON map.grid_id = grid_key.grid_id JOIN keymap ON grid_key.key_name = keymap.key_name; ", "");

                var bounds = new Metadata();
                bounds.name = "bounds";
                bounds.value = "-180,-85.0511,180,85.0511";
                conn.InsertOrReplace(bounds);

                var center = new Metadata();
                center.name = "center";
                center.value = "0,0,2";
                conn.InsertOrReplace(center);

                var minzoom = new Metadata();
                minzoom.name = "minzoom";
                var zoomscales = tileSource.Schema.Resolutions.Keys.Select(x => Convert.ToInt32(x)).ToArray();
                minzoom.value = zoomscales.Min().ToString();
                conn.InsertOrReplace(minzoom);

                var maxzoom = new Metadata();
                maxzoom.name = "maxzoom";
                maxzoom.value = zoomscales.Max().ToString();
                conn.InsertOrReplace(maxzoom);

                var name = new Metadata();
                name.name = "name";
                name.value = layer.title;
                conn.InsertOrReplace(name);

                var description = new Metadata();
                description.name = "description";
                description.value = layer.title;
                conn.InsertOrReplace(description);

                var template = new Metadata();
                template.name = "template";
                template.value = String.Empty;
                conn.InsertOrReplace(template);

                var version = new Metadata();
                version.name = "version";
                version.value = "1.0.0";
                conn.InsertOrReplace(version);
            }
        }

        /// <summary>
        /// Add a tile to an mbtiles database
        /// </summary>
        /// <param name="image"></param>
        /// <param name="tileInfo"></param>
        /// <param name="dbpath"></param>
        public static void PopulateMbtilesWith(byte[] image, TileInfo tileInfo, string dbpath)
        {
            var tileId = Guid.NewGuid().ToString().ToLower();

            var connection = new SQLiteConnection(dbpath);

            using (SQLiteConnection conn = connection)
            {
                var imageTable = new Images();
                imageTable.tile_id = tileId;
                imageTable.tile_data = image;
                conn.InsertOrReplace(imageTable);

                var map = new Map();
                map.tile_id = tileId;
                map.zoom_level = Convert.ToInt16(tileInfo.Index.Level);
                map.tile_column = tileInfo.Index.Col;
                map.tile_row = (int)Math.Pow(2,map.zoom_level) - 1 - tileInfo.Index.Row;
                conn.InsertOrReplace(map);
            }
            
        }

        /// <summary>
        /// Check if tile exists before adding it
        /// </summary>
        /// <param name="tileInfo"></param>
        /// <param name="dbpath"></param>
        /// <returns>If tile exists</returns>
        public static bool TileExists(TileInfo tileInfo, string dbpath)
        {
            using (SQLiteConnection conn = new SQLiteConnection(dbpath))
            {
                var existingTile = conn.Table<Map>().Where(Tile => Tile.tile_column == tileInfo.Index.Col).Where(Tile => Tile.tile_row == tileInfo.Index.Row);
                return existingTile == null;
            }
        }
    }
}