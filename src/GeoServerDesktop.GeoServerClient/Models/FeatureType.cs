using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// 表示 GeoServer 要素类型
    /// </summary>
    public class FeatureType
    {
        /// <summary>
        /// 初始化 FeatureType 类的新实例（FIXED-E27：Enabled 默认 true，
        /// 与本客户端其余资源模型的创建语义及 GeoServer 默认一致，
        /// 消除"直调创建即默认禁用"缺陷）
        /// </summary>
        public FeatureType()
        {
            Enabled = true;
        }

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
        /// 要素类型是否启用（FIXED-E27：bool? 对齐其余资源模型；构造函数默认 true）
        /// </summary>
        [JsonProperty("enabled")]
        public bool? Enabled { get; set; }

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

        /// <summary>
        /// 属性面（几何字段 + 业务字段）。GeoServer 返回形态为 {"attribute":[{...},...]}，
        /// 由 <see cref="FeatureTypeAttributeConverter"/> 兼容“包装对象 / 裸数组 / 单对象 / 缺省”。
        /// 发布后校验依赖它判断服务端是否真的解析出数据：实测原始名未匹配到磁盘文件时属性面为空，
        /// 而只判 HTTP 2xx 会让调用方误以为发布成功。
        /// </summary>
        [JsonProperty("attributes")]
        [JsonConverter(typeof(FeatureTypeAttributeConverter))]
        public List<FeatureAttributeInfo> Attributes { get; set; }
    }

    /// <summary>
    /// 要素类型的单个属性（字段）描述
    /// </summary>
    public class FeatureAttributeInfo
    {
        /// <summary>
        /// 字段名（几何字段通常为 the_geom）
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Java 绑定类型（如 java.lang.String、org.locationtech.jts.geom.MultiPolygon）
        /// </summary>
        [JsonProperty("binding")]
        public string Binding { get; set; }

        /// <summary>
        /// 是否可为空
        /// </summary>
        [JsonProperty("nillable")]
        public bool? Nillable { get; set; }

        /// <summary>
        /// 最小出现次数
        /// </summary>
        [JsonProperty("minOccurs")]
        public int? MinOccurs { get; set; }

        /// <summary>
        /// 最大出现次数
        /// </summary>
        [JsonProperty("maxOccurs")]
        public int? MaxOccurs { get; set; }

        /// <summary>
        /// 字符字段长度
        /// </summary>
        [JsonProperty("length")]
        public int? Length { get; set; }
    }

    /// <summary>
    /// 把 GeoServer 的 {"attribute":[...]} 包装形态读成 List，写回时保持同一包装形态
    /// </summary>
    public class FeatureTypeAttributeConverter : JsonConverter
    {
        /// <summary>
        /// 是否支持该类型
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<FeatureAttributeInfo>);
        }

        /// <summary>
        /// 只负责读取：写出必须交回默认序列化器。
        /// 自定义 WriteJson 会抢在 NullValueHandling.Ignore 之前执行，导致创建请求里
        /// 恒定带上 "attributes" 包装体——实测 GeoServer 2.28 对该形态直接 500（3.0.1 容忍），
        /// 属跨版本兼容风险，故此处不允许写。
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// 读取属性面
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.Array) return token.ToObject<List<FeatureAttributeInfo>>(serializer);
            if (token.Type == JTokenType.Object)
            {
                var inner = token["attribute"];
                if (inner == null || inner.Type == JTokenType.Null)
                    return new List<FeatureAttributeInfo> { token.ToObject<FeatureAttributeInfo>(serializer) };
                if (inner.Type == JTokenType.Array) return inner.ToObject<List<FeatureAttributeInfo>>(serializer);
                return new List<FeatureAttributeInfo> { inner.ToObject<FeatureAttributeInfo>(serializer) };
            }
            return null;
        }

        /// <summary>
        /// 写入属性面（保持 attribute 包装）
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("attribute");
            serializer.Serialize(writer, value);
            writer.WriteEndObject();
        }
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
        /// 坐标参考系统（GeoServer 可能返回字符串或 {"@class","$"} 引用对象，统一宽容为字符串）
        /// </summary>
        [JsonProperty("crs")]
        [JsonConverter(typeof(CrsStringConverter))]
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

    /// <summary>
    /// 宽容 CRS 字符串：接受字符串或 {"@class":...,"$":"..."} 引用对象，统一转为字符串。
    /// GeoServer 3.0.1 对 coverage 的 nativeCRS / 边界框 crs 返回对象形态，对部分资源返回字符串形态。
    /// </summary>
    internal sealed class CrsStringConverter : JsonConverter
    {
        /// <summary>仅处理字符串属性的转换。</summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        /// <summary>读取：字符串原样返回；引用对象取 "$" 值；null 返回 null。</summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            if (reader.TokenType == JsonToken.String) return (string)reader.Value;
            if (reader.TokenType == JsonToken.StartObject)
            {
                var jo = JObject.Load(reader);
                var dollar = jo["$"];
                return dollar == null ? null : dollar.ToString();
            }
            reader.Skip();
            return null;
        }

        /// <summary>写入：字符串原样写出。</summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((string)value);
        }
    }
}
