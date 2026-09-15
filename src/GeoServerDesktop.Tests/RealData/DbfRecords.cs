using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>
    /// 极小 DBF 数据区读取器：把 .dbf 逐条记录读成列名字典（独立于 GDAL/GeoServer）。
    /// 记录布局：每记录 1 字节删除标记（0x20 正常 / 0x2A 已删除）+ 各字段定长 ASCII。
    /// 与 DbfHeader 的字段定义配合，用于从数据文件独立推导 WFS/WMS 属性期望值。
    /// </summary>
    public static class DbfRecords
    {
        public static List<Dictionary<string, string>> Read(string dbfPath)
        {
            var h = DbfHeader.Parse(dbfPath);
            var bytes = File.ReadAllBytes(dbfPath);
            var rows = new List<Dictionary<string, string>>();
            for (int i = 0; i < h.RecordCount; i++)
            {
                int start = h.HeaderLength + i * h.RecordLength;
                if (start + h.RecordLength > bytes.Length) break;
                if (bytes[start] == 0x2A) continue;                    // '*' = 逻辑删除，跳过
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int off = start + 1;                                    // 跳过删除标记
                foreach (var f in h.Fields)
                {
                    var raw = Encoding.ASCII.GetString(bytes, off, f.Length);
                    off += f.Length;
                    dict[f.Name] = raw.Trim();
                }
                rows.Add(dict);
            }
            return rows;
        }

        /// <summary>取某列的 int 值（去空白，空→0）。</summary>
        public static int GetInt(List<Dictionary<string, string>> rows, string col, int index)
            => int.TryParse(rows[index][col].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

        /// <summary>取某列的 double 值（InvariantCulture，兼容 "20.500000000000000"）。</summary>
        public static double GetDouble(List<Dictionary<string, string>> rows, string col, int index)
            => double.TryParse(rows[index][col].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;

        public static string GetStr(List<Dictionary<string, string>> rows, string col, int index) => rows[index][col].Trim();
    }
}
