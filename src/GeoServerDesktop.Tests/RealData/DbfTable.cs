using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GeoServerDesktop.Tests.RealData
{
    /// <summary>DBF 编码取用策略（期望值必须从数据文件自身推导，不接受从服务回抄）。</summary>
    public enum DbfEncodingMode
    {
        /// <summary>优先 .cpg 声明；无声明时按 UTF-8 → GBK → ISO-8859-1 探测（“人类正确语义”真值）。</summary>
        DeclaredOrDetected,

        /// <summary>仅认 .cpg 声明，无声明一律 ISO-8859-1（模拟服务端默认解码，用于诊断差异归因）。</summary>
        DeclaredOnly,

        /// <summary>强制指定（诊断用）。</summary>
        Forced,
    }

    /// <summary>单个 DBF 字段值（按字段类型归一）。IsNull 覆盖“全空格/全零”文本与 '?' 逻辑值与空数值。</summary>
    public readonly struct DbfCell
    {
        public readonly bool IsNull;
        public readonly char Type;            // C / N / F / D / L
        public readonly string? Text;         // 解码后的文本（数值型为原始字面）
        public readonly double Num;
        public readonly bool? Bool;
        public readonly string? DateIso;      // yyyy-MM-dd

        public DbfCell(bool isNull, char type, string? text, double num, bool? b, string? dateIso)
        {
            IsNull = isNull; Type = type; Text = text; Num = num; Bool = b; DateIso = dateIso;
        }

        public override string ToString()
            => IsNull ? "NULL" : (Type == 'D' ? DateIso! : Type == 'L' ? (Bool == true ? "T" : "F") : Text!);
    }

    /// <summary>
    /// 独立 DBF 表读取器：字节级解析 + 编码感知 + 按字段类型归一。
    /// 不经 GDAL、不经被测客户端，供真实数据属性保真检查推导期望值。
    /// </summary>
    public sealed class DbfTable
    {
        private static bool _codePagesRegistered;

        private static void EnsureCodePages()
        {
            if (_codePagesRegistered) return;
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
            _codePagesRegistered = true;
        }

        public string Path { get; private set; } = "";
        public int RecordCount { get; private set; }
        public string[] FieldNames { get; private set; } = Array.Empty<string>();
        public char[] FieldTypes { get; private set; } = Array.Empty<char>();
        public int[] FieldWidths { get; private set; } = Array.Empty<int>();
        public int[] FieldDecimals { get; private set; } = Array.Empty<int>();
        /// <summary>记录序（跳过 '*' 逻辑删除）→ 每字段的单元格。</summary>
        public List<DbfCell[]> Rows { get; } = new List<DbfCell[]>();

        public string EncodingName { get; private set; } = "ISO-8859-1";
        public bool CpgPresent { get; private set; }
        public string? CpgRaw { get; private set; }
        /// <summary>.cpg 是否存在（false = 无编码声明）。</summary>
        /// <summary>探测到的编码是否与 .cpg 缺失有关（诊断“无声明”数据集）。</summary>
        public bool EncodingInferred { get; private set; }

        public int IndexOf(string field)
        {
            for (int i = 0; i < FieldNames.Length; i++)
                if (string.Equals(FieldNames[i], field, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        public DbfCell Cell(int row, string field)
        {
            int i = IndexOf(field);
            if (i < 0) throw new InvalidDataException("DBF 无该字段 " + field + "（现有 " + string.Join(",", FieldNames) + "）");
            return Rows[row][i];
        }

        public string? Str(int row, string field) { var c = Cell(row, field); return c.IsNull ? null : c.Text; }
        public double Dbl(int row, string field) { var c = Cell(row, field); return c.IsNull ? double.NaN : c.Num; }
        public long Lng(int row, string field) { var c = Cell(row, field); return c.IsNull ? 0 : (long)Math.Round(c.Num); }
        public bool? Bl(int row, string field) { var c = Cell(row, field); return c.IsNull ? (bool?)null : c.Bool; }
        public string? Date(int row, string field) { var c = Cell(row, field); return c.IsNull ? null : c.DateIso; }

        // ---------------- 编码解析 ----------------

        /// <summary>读同名 .cpg（GeoServer/GeoTools 与 GDAL 的编码声明通道）。</summary>
        public static string? ReadCpg(string dbfPath)
        {
            var cpg = System.IO.Path.ChangeExtension(dbfPath, ".cpg");
            if (!File.Exists(cpg)) return null;
            try
            {
                var bytes = File.ReadAllBytes(cpg);
                var s = new StringBuilder(bytes.Length);
                foreach (var b in bytes)
                {
                    if (b == 0x0D || b == 0x0A || b == 0x20) continue;
                    if (b == 0x2D || b == 0x5F) { s.Append('-'); continue; }   // GB2312 / GB_2312 归一
                    s.Append((char)b);
                }
                var norm = s.ToString().Trim().ToUpperInvariant();
                return norm.Length == 0 ? null : norm;
            }
            catch { return null; }
        }

        public static string? NormalizeEncodingName(string? cpg)
        {
            if (string.IsNullOrEmpty(cpg)) return null;
            switch (cpg.Replace("-", "").Replace("_", "").ToUpperInvariant())
            {
                case "UTF8": return "UTF-8";
                case "GBK":
                case "CP936":
                case "GB2312":
                case "EUCGB2312": return "GBK";
                case "BIG5": return "BIG5";
                case "ISO88591":
                case "88591":
                case "LATIN1": return "ISO-8859-1";
                case "WINDOWS1252":
                case "CP1252":
                case "1252": return "Windows-1252";
                case "UTF16LE":
                case "UCS2LE": return "UTF-16LE";
                default: return cpg;
            }
        }

        public static Encoding GetEncoding(string? name)
        {
            EnsureCodePages();
            try { return Encoding.GetEncoding(name ?? "ISO-8859-1"); }
            catch { return Encoding.GetEncoding("ISO-8859-1"); }
        }

        /// <summary>无 .cpg 时的字节级探测：严格 UTF-8 → 严格 GBK → ISO-8859-1。</summary>
        public static string DetectEncodingName(IEnumerable<byte[]> textFieldBytes)
        {
            var all = new List<byte>();
            foreach (var f in textFieldBytes) all.AddRange(f);
            bool anyHigh = all.Any(b => b >= 0x80);
            if (!anyHigh) return "ISO-8859-1";
            if (DecodesStrict(Encoding.UTF8, all)) return "UTF-8";
            EnsureCodePages();
            try
            {
                var gbk = Encoding.GetEncoding("GBK", EncoderFallback.ReplacementFallback, new DecoderExceptionFallback());
                if (DecodesStrict(gbk, all)) return "GBK";
            }
            catch { }
            return "ISO-8859-1";
        }

        private static bool DecodesStrict(Encoding enc, List<byte> bytes)
        {
            try
            {
                var withThrow = Encoding.GetEncoding(enc.WebName,
                    EncoderFallback.ReplacementFallback, new DecoderExceptionFallback());
                withThrow.GetString(bytes.ToArray());
                return true;
            }
            catch (DecoderFallbackException) { return false; }
            catch (ArgumentException) { return false; }
        }

        // ---------------- 装载 ----------------

        public static DbfTable Load(string dbfPath, DbfEncodingMode mode = DbfEncodingMode.DeclaredOrDetected,
                                    string? forcedEncoding = null)
        {
            var bytes = File.ReadAllBytes(dbfPath);
            if (bytes.Length < 32 || bytes[0] != 0x03)
                throw new InvalidDataException("非法 DBF（首字节应为 0x03）: " + dbfPath);
            var t = new DbfTable
            {
                Path = dbfPath,
                RecordCount = BitConverter.ToInt32(bytes, 4),
                CpgRaw = ReadCpg(dbfPath),
            };
            t.CpgPresent = t.CpgRaw != null;
            int headerLen = BitConverter.ToInt16(bytes, 8);
            int recordLen = BitConverter.ToInt16(bytes, 10);

            var names = new List<string>(); var types = new List<char>();
            var widths = new List<int>(); var decs = new List<int>();
            int pos = 32;
            while (pos + 32 <= bytes.Length && bytes[pos] != 0x0D)
            {
                var nm = Encoding.ASCII.GetString(bytes, pos, 11);
                int nul = nm.IndexOf('\0');
                names.Add((nul >= 0 ? nm.Substring(0, nul) : nm).Trim());
                types.Add((char)bytes[pos + 11]);
                widths.Add(bytes[pos + 16]);
                decs.Add(bytes[pos + 17]);
                pos += 32;
            }
            t.FieldNames = names.ToArray();
            t.FieldTypes = types.ToArray();
            t.FieldWidths = widths.ToArray();
            t.FieldDecimals = decs.ToArray();

            // 1) 先取每个 'C' 字段的原始字节，决定编码
            var textFieldChunks = new List<byte[]>();
            for (int i = 0; i < t.RecordCount; i++)
            {
                int start = headerLen + i * recordLen;
                if (start + recordLen > bytes.Length) break;
                if (bytes[start] == 0x2A) continue;
                int off = start + 1;
                for (int f = 0; f < names.Count; f++)
                {
                    if (types[f] == 'C' || types[f] == 'V')
                        textFieldChunks.Add(Slice(bytes, off, widths[f]));
                    off += widths[f];
                }
            }

            string? cpgNorm = NormalizeEncodingName(t.CpgRaw);
            if (mode == DbfEncodingMode.Forced)
            {
                t.EncodingName = NormalizeEncodingName(forcedEncoding) ?? forcedEncoding ?? "ISO-8859-1";
                t.EncodingInferred = true;
            }
            else if (cpgNorm != null)
            {
                t.EncodingName = cpgNorm;
                t.EncodingInferred = false;
            }
            else if (mode == DbfEncodingMode.DeclaredOnly)
            {
                t.EncodingName = "ISO-8859-1";
                t.EncodingInferred = true;
            }
            else
            {
                t.EncodingName = DetectEncodingName(textFieldChunks);
                t.EncodingInferred = true;
            }
            var textEnc = GetEncoding(t.EncodingName);

            // 2) 逐记录逐字段归一
            for (int i = 0; i < t.RecordCount; i++)
            {
                int start = headerLen + i * recordLen;
                if (start + recordLen > bytes.Length) break;
                if (bytes[start] == 0x2A) continue;                       // 逻辑删除
                int off = start + 1;
                var cells = new DbfCell[names.Count];
                for (int f = 0; f < names.Count; f++)
                {
                    var raw = Slice(bytes, off, widths[f]);
                    off += widths[f];
                    cells[f] = Decode(raw, types[f], textEnc);
                }
                t.Rows.Add(cells);
            }
            return t;
        }

        private static byte[] Slice(byte[] b, int off, int len)
        {
            if (off + len > b.Length) len = Math.Max(0, b.Length - off);
            var r = new byte[len];
            Array.Copy(b, off, r, 0, len);
            return r;
        }

        private static DbfCell Decode(byte[] raw, char type, Encoding textEnc)
        {
            switch (type)
            {
                case 'C':
                case 'V':
                    {
                        bool nul = raw.All(b => b == 0x20 || b == 0x00);
                        if (nul) return new DbfCell(true, 'C', null, 0, null, null);
                        // 文本字段以 0x00 截断（部分写手用零填充而非空格）
                        int end = raw.Length;
                        for (int i = 0; i < raw.Length; i++) if (raw[i] == 0x00) { end = i; break; }
                        var s = textEnc.GetString(raw, 0, end).Trim();
                        return new DbfCell(s.Length == 0, 'C', s, 0, null, null);
                    }
                case 'N':
                case 'F':
                    {
                        var s = Encoding.ASCII.GetString(raw).Trim();
                        if (s.Length == 0 || s.All(c => c == '*'))
                            return new DbfCell(true, type, null, double.NaN, null, null);
                        double d = double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                            ? v : double.NaN;
                        return new DbfCell(double.IsNaN(d), type, s, d, null, null);
                    }
                case 'D':
                    {
                        var s = Encoding.ASCII.GetString(raw).Trim();
                        if (s.Length < 8 || !DateTime.TryParseExact(s.Substring(0, 8), "yyyyMMdd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                            return new DbfCell(true, 'D', null, 0, null, null);
                        return new DbfCell(false, 'D', s, 0, null, dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    }
                case 'L':
                    {
                        char c = raw.Length == 0 ? '?' : (char)raw[0];
                        if (c == 'T' || c == 't' || c == 'Y' || c == 'y') return new DbfCell(false, 'L', "T", 1, true, null);
                        if (c == 'F' || c == 'f' || c == 'N' || c == 'n') return new DbfCell(false, 'L', "F", 0, false, null);
                        return new DbfCell(true, 'L', null, 0, null, null);
                    }
                default:
                    {
                        var s = textEnc.GetString(raw).Trim();
                        return new DbfCell(s.Length == 0, type, s, 0, null, null);
                    }
            }
        }
    }
}
