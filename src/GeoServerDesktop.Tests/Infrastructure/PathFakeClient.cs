using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;

namespace GeoServerDesktop.Tests.Infrastructure
{
    /// <summary>
    /// 按请求路径应答的假 HTTP 客户端（M4 聚合服务多路 GET 交错场景）：
    /// 显式登记的路径命中即返回（每次命中出队，支持异常交错）；未登记路径回落
    /// <see cref="SetDefault"/>（动词维度）或 "{}"。所有请求按序记录。
    /// </summary>
    public sealed class PathFakeClient : IGeoServerHttpClient
    {
        public sealed class Recorded
        {
            public string Method;      // GET/POST/PUT/DELETE
            public string Path;        // 原始传入 path（未拼接）
            public string Body;        // 请求体文本（可为 null）
            public string ContentType; // 请求体 Content-Type（可为 null）
        }

        public readonly List<Recorded> Requests = new List<Recorded>();

        private readonly Dictionary<string, Queue<object>> _byPath =
            new Dictionary<string, Queue<object>>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _defaults =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Recorded Last
        {
            get { return Requests.Count > 0 ? Requests[Requests.Count - 1] : null; }
        }

        public Recorded First(string verb)
        {
            foreach (var r in Requests) if (r.Method == verb) return r;
            return null;
        }

        public List<Recorded> All(string verb)
        {
            var list = new List<Recorded>();
            foreach (var r in Requests) if (r.Method == verb) list.Add(r);
            return list;
        }

        /// <summary>登记路径响应（按登记顺序消费）。</summary>
        public PathFakeClient RespondGet(string path, string json) { Enqueue("GET " + path, json); return this; }

        /// <summary>登记路径异常（与响应按登记顺序交错消费）。</summary>
        public PathFakeClient ThrowGet(string path, Exception ex) { Enqueue("GET " + path, ex); return this; }

        /// <summary>登记 GET 路径 404（GeoServerRequestException("Not Found",404,null)）。</summary>
        public PathFakeClient NotFoundGet(string path) =>
            ThrowGet(path, new GeoServerDesktop.GeoServerClient.Http.GeoServerRequestException("Not Found", 404, null));

        /// <summary>登记 PUT 路径响应。</summary>
        public PathFakeClient RespondPut(string path, string json) { Enqueue("PUT " + path, json); return this; }

        /// <summary>登记 POST 路径响应。</summary>
        public PathFakeClient RespondPost(string path, string json) { Enqueue("POST " + path, json); return this; }

        private void Enqueue(string key, object item)
        {
            if (!_byPath.TryGetValue(key, out var q)) { q = new Queue<object>(); _byPath[key] = q; }
            q.Enqueue(item);
        }

        private async Task<string> Record(string verb, string path, HttpContent content)
        {
            var r = new Recorded { Method = verb, Path = path };
            if (content != null)
            {
                try { r.ContentType = content.Headers?.ContentType?.ToString(); } catch { }
                try { r.Body = await content.ReadAsStringAsync(); } catch { r.Body = "<unreadable>"; }
            }
            Requests.Add(r);

            var key = verb + " " + path;
            if (_byPath.TryGetValue(key, out var q) && q.Count > 0)
            {
                var item = q.Dequeue();
                if (item is Exception ex) throw ex;
                return (string)item;
            }
            return _defaults.TryGetValue(verb, out var d) ? d : "{}";
        }

        public void SetDefault(string verb, string json) => _defaults[verb] = json;

        public Task<string> GetAsync(string path, CancellationToken cancellationToken = default) =>
            Record("GET", path, null);

        public Task<string> PostAsync(string path, HttpContent content, CancellationToken cancellationToken = default) =>
            Record("POST", path, content);

        public Task<string> PutAsync(string path, HttpContent content, CancellationToken cancellationToken = default) =>
            Record("PUT", path, content);

        public Task<string> DeleteAsync(string path, CancellationToken cancellationToken = default) =>
            Record("DELETE", path, null);

        public void Dispose() { }
    }
}
