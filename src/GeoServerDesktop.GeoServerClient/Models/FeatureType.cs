using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 要素类型
    /// </summary>
    public class FeatureType
    {
        /// <summary>
        /// 要素类型的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 要素类型的本地名称
        /// </summary>
        [JsonProperty("nativeName")]
        public string NativeName { get; set; }

        /// <summary>
        /// 与要素类型关联的命名空间
        /// </summary>
        [JsonProperty("namespace")]
        public NamespaceReference Namespace { get; set; }

        /// <summary>
        /// 要素类型的标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 要素类型的摘要/描述
        /// </summary>
        [JsonProperty("abstract")]
        public string Abstract { get; set; }

        /// <summary>
        /// 与要素类型关联的关键字
        /// </summary>
        [JsonProperty("keywords")]
        public KeywordInfo Keywords { get; set; }

        /// <summary>
        /// 要素类型的本地坐标参考系统
        /// </summary>
        [JsonProperty("nativeCRS")]
        public string NativeCRS { get; set; }

        /// <summary>
        /// 要素类型的空间参考系统
        /// </summary>
        [JsonProperty("srs")]
        public string Srs { get; set; }

        /// <summary>
        /// 本地边界框
        /// </summary>
        [JsonProperty("nativeBoundingBox")]
        public BoundingBox NativeBoundingBox { get; set; }

        /// <summary>
        /// 经纬度边界框
        /// </summary>
        [JsonProperty("latLonBoundingBox")]
        public BoundingBox LatLonBoundingBox { get; set; }

        /// <summary>
        /// 要素类型是否启用
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// 数据存储引用
        /// </summary>
        [JsonProperty("store")]
        public StoreReference Store { get; set; }

        /// <summary>
        /// 要素类型资源的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 对命名空间的引用
    /// </summary>
    public class NamespaceReference
    {
        /// <summary>
        /// 命名空间的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 命名空间的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 对数据存储的引用
    /// </summary>
    public class StoreReference
    {
        /// <summary>
        /// 数据存储的类型
        /// </summary>
        [JsonProperty("@class")]
        public string Class { get; set; }

        /// <summary>
        /// 数据存储的名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 数据存储的链接
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    /// <summary>
    /// 关键字信息
    /// </summary>
    public class KeywordInfo
    {
        /// <summary>
        /// 关键字数组
        /// </summary>
        [JsonProperty("string")]
        public string[] Keywords { get; set; }
    }

    /// <summary>
    /// 边界框信息
    /// </summary>
    public class BoundingBox
    {
        /// <summary>
        /// 最小 X 坐标
        /// </summary>
        [JsonProperty("minx")]
        public double MinX { get; set; }

        /// <summary>
        /// 最大 X 坐标
        /// </summary>
        [JsonProperty("maxx")]
        public double MaxX { get; set; }

        /// <summary>
        /// 最小 Y 坐标
        /// </summary>
        [JsonProperty("miny")]
        public double MinY { get; set; }

        /// <summary>
        /// 最大 Y 坐标
        /// </summary>
        [JsonProperty("maxy")]
        public double MaxY { get; set; }

        /// <summary>
        /// 坐标参考系统
        /// </summary>
        [JsonProperty("crs")]
        public string Crs { get; set; }
    }

    /// <summary>
    /// 单个要素类型响应的包装器
    /// </summary>
    public class FeatureTypeWrapper
    {
        /// <summary>
        /// 要素类型数据
        /// </summary>
        [JsonProperty("featureType")]
        public FeatureType FeatureType { get; set; }
    }

    /// <summary>
    /// 要素类型列表
    /// </summary>
    public class FeatureTypeList
    {
        /// <summary>
        /// 要素类型数组
        /// </summary>
        [JsonProperty("featureType")]
        public FeatureType[] FeatureTypes { get; set; }
    }

    /// <summary>
    /// 要素类型列表响应的包装器
    /// </summary>
    public class FeatureTypeListWrapper
    {
        /// <summary>
        /// 要素类型列表数据
        /// </summary>
        [JsonProperty("featureTypes")]
        public FeatureTypeList FeatureTypeList { get; set; }
    }
}
