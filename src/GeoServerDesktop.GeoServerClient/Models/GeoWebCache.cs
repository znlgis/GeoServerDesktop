using Newtonsoft.Json;
using System.Collections.Generic;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Wrapper for GeoWebCache layers list
    /// </summary>
    public class GWCLayerListWrapper
    {
        /// <summary>
        /// Gets or sets the list of cached layers
        /// </summary>
        [JsonProperty("layers")]
        public List<string> Layers { get; set; }
    }

    /// <summary>
    /// Represents a GeoWebCache layer
    /// </summary>
    public class GWCLayer
    {
        /// <summary>
        /// Gets or sets the layer name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the layer is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the MIME formats
        /// </summary>
        [JsonProperty("mimeFormats")]
        public List<string> MimeFormats { get; set; }

        /// <summary>
        /// Gets or sets the gridsets
        /// </summary>
        [JsonProperty("gridSubsets")]
        public List<GridSubset> GridSubsets { get; set; }

        /// <summary>
        /// Gets or sets the metadata URLs
        /// </summary>
        [JsonProperty("metaWidthHeight")]
        public int[] MetaWidthHeight { get; set; }

        /// <summary>
        /// Gets or sets the expiration time in seconds
        /// </summary>
        [JsonProperty("expireCache")]
        public int? ExpireCache { get; set; }

        /// <summary>
        /// Gets or sets the expiration clients
        /// </summary>
        [JsonProperty("expireClients")]
        public int? ExpireClients { get; set; }
    }

    /// <summary>
    /// Represents a grid subset
    /// </summary>
    public class GridSubset
    {
        /// <summary>
        /// Gets or sets the gridset name
        /// </summary>
        [JsonProperty("gridSetName")]
        public string GridSetName { get; set; }

        /// <summary>
        /// Gets or sets the zoom start level
        /// </summary>
        [JsonProperty("zoomStart")]
        public int? ZoomStart { get; set; }

        /// <summary>
        /// Gets or sets the zoom stop level
        /// </summary>
        [JsonProperty("zoomStop")]
        public int? ZoomStop { get; set; }

        /// <summary>
        /// Gets or sets the extent
        /// </summary>
        [JsonProperty("extent")]
        public Extent Extent { get; set; }
    }

    /// <summary>
    /// Represents an extent/bounding box
    /// </summary>
    public class Extent
    {
        /// <summary>
        /// Gets or sets the coordinates
        /// </summary>
        [JsonProperty("coords")]
        public double[] Coords { get; set; }
    }

    /// <summary>
    /// Represents seeding request
    /// </summary>
    public class SeedRequest
    {
        /// <summary>
        /// Gets or sets the seed request wrapper
        /// </summary>
        [JsonProperty("seedRequest")]
        public SeedRequestConfig Config { get; set; }
    }

    /// <summary>
    /// Seed request configuration
    /// </summary>
    public class SeedRequestConfig
    {
        /// <summary>
        /// Gets or sets the layer name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the gridset ID
        /// </summary>
        [JsonProperty("gridSetId")]
        public string GridSetId { get; set; }

        /// <summary>
        /// Gets or sets the zoom start level
        /// </summary>
        [JsonProperty("zoomStart")]
        public int ZoomStart { get; set; }

        /// <summary>
        /// Gets or sets the zoom stop level
        /// </summary>
        [JsonProperty("zoomStop")]
        public int ZoomStop { get; set; }

        /// <summary>
        /// Gets or sets the format
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the type (seed, reseed, truncate)
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the thread count
        /// </summary>
        [JsonProperty("threadCount")]
        public int? ThreadCount { get; set; }
    }

    /// <summary>
    /// Represents disk quota configuration
    /// </summary>
    public class DiskQuotaConfig
    {
        /// <summary>
        /// Gets or sets whether disk quota is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the disk block size
        /// </summary>
        [JsonProperty("diskBlockSize")]
        public int? DiskBlockSize { get; set; }

        /// <summary>
        /// Gets or sets the cache cleanup frequency
        /// </summary>
        [JsonProperty("cacheCleanUpFrequency")]
        public int? CacheCleanUpFrequency { get; set; }

        /// <summary>
        /// Gets or sets the cache cleanup units
        /// </summary>
        [JsonProperty("cacheCleanUpUnits")]
        public string CacheCleanUpUnits { get; set; }

        /// <summary>
        /// Gets or sets the maximum cache age
        /// </summary>
        [JsonProperty("maxConcurrentCleanUps")]
        public int? MaxConcurrentCleanUps { get; set; }

        /// <summary>
        /// Gets or sets the global quota
        /// </summary>
        [JsonProperty("globalQuota")]
        public Quota GlobalQuota { get; set; }

        /// <summary>
        /// Gets or sets the layer quotas
        /// </summary>
        [JsonProperty("layerQuotas")]
        public List<LayerQuota> LayerQuotas { get; set; }
    }

    /// <summary>
    /// Represents a quota value
    /// </summary>
    public class Quota
    {
        /// <summary>
        /// Gets or sets the value
        /// </summary>
        [JsonProperty("value")]
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the units (B, KB, MB, GB, TB)
        /// </summary>
        [JsonProperty("units")]
        public string Units { get; set; }
    }

    /// <summary>
    /// Represents a layer-specific quota
    /// </summary>
    public class LayerQuota
    {
        /// <summary>
        /// Gets or sets the layer name
        /// </summary>
        [JsonProperty("layer")]
        public string Layer { get; set; }

        /// <summary>
        /// Gets or sets the quota
        /// </summary>
        [JsonProperty("quota")]
        public Quota Quota { get; set; }
    }

    /// <summary>
    /// Represents a gridset definition
    /// </summary>
    public class Gridset
    {
        /// <summary>
        /// Gets or sets the gridset name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the SRS
        /// </summary>
        [JsonProperty("srs")]
        public SRS SRS { get; set; }

        /// <summary>
        /// Gets or sets the extent
        /// </summary>
        [JsonProperty("extent")]
        public Extent Extent { get; set; }

        /// <summary>
        /// Gets or sets whether alignment is top-left
        /// </summary>
        [JsonProperty("alignTopLeft")]
        public bool? AlignTopLeft { get; set; }

        /// <summary>
        /// Gets or sets the resolutions
        /// </summary>
        [JsonProperty("resolutions")]
        public List<double> Resolutions { get; set; }

        /// <summary>
        /// Gets or sets the meters per unit
        /// </summary>
        [JsonProperty("metersPerUnit")]
        public double? MetersPerUnit { get; set; }

        /// <summary>
        /// Gets or sets the pixel size
        /// </summary>
        [JsonProperty("pixelSize")]
        public double? PixelSize { get; set; }

        /// <summary>
        /// Gets or sets the scale names
        /// </summary>
        [JsonProperty("scaleNames")]
        public List<string> ScaleNames { get; set; }

        /// <summary>
        /// Gets or sets the tile height
        /// </summary>
        [JsonProperty("tileHeight")]
        public int? TileHeight { get; set; }

        /// <summary>
        /// Gets or sets the tile width
        /// </summary>
        [JsonProperty("tileWidth")]
        public int? TileWidth { get; set; }

        /// <summary>
        /// Gets or sets whether Y coordinate increases downward
        /// </summary>
        [JsonProperty("yCoordinateFirst")]
        public bool? YCoordinateFirst { get; set; }
    }

    /// <summary>
    /// Represents SRS information
    /// </summary>
    public class SRS
    {
        /// <summary>
        /// Gets or sets the SRS number
        /// </summary>
        [JsonProperty("number")]
        public int Number { get; set; }
    }

    /// <summary>
    /// Wrapper for gridsets list
    /// </summary>
    public class GridsetListWrapper
    {
        /// <summary>
        /// Gets or sets the list of gridsets
        /// </summary>
        [JsonProperty("gridSets")]
        public List<string> GridSets { get; set; }
    }

    /// <summary>
    /// Represents a blobstore configuration
    /// </summary>
    public class Blobstore
    {
        /// <summary>
        /// Gets or sets the blobstore ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the blobstore type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets whether the blobstore is enabled
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the base directory for file blobstores
        /// </summary>
        [JsonProperty("baseDirectory")]
        public string BaseDirectory { get; set; }

        /// <summary>
        /// Gets or sets the file system block size
        /// </summary>
        [JsonProperty("fileSystemBlockSize")]
        public int? FileSystemBlockSize { get; set; }

        /// <summary>
        /// Gets or sets additional configuration properties
        /// </summary>
        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; }
    }

    /// <summary>
    /// Wrapper for blobstore list
    /// </summary>
    public class BlobstoreListWrapper
    {
        /// <summary>
        /// Gets or sets the list of blobstores
        /// </summary>
        [JsonProperty("blobstores")]
        public List<string> Blobstores { get; set; }
    }

    /// <summary>
    /// Wrapper for blobstore response
    /// </summary>
    public class BlobstoreWrapper
    {
        /// <summary>
        /// Gets or sets the blobstore
        /// </summary>
        [JsonProperty("blobstore")]
        public Blobstore Blobstore { get; set; }
    }
}
