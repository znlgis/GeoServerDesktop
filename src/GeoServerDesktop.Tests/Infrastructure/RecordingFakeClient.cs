using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GeoServerDesktop.GeoServerClient.Http;

namespace GeoServerDesktop.Tests.Infrastructure
{
    /// <summary>记录型假 HTTP 客户端：捕获请求动词/路径/请求体，返回预设响应。用于离线单元测试。</summary>
    public sealed class RecordingFakeClient : IGeoServerHttpClient
    {
        public sealed class Recorded
        {
            public string Method;      // GET/POST/PUT/DELETE
            public string Path;        // 原始传入 path（未拼接）
            public string Body;        // 请求体文本（可为 null）
            public string ContentType; // 请求体 Content-Type（可为 null）
            public byte[] RawBody;     // 原始字节（ByteArrayContent 或文本转字节）
        }

        public readonly List<Recorded> Requests = new List<Recorded>();
        /// <summary>按动词的响应队列（出队）；无预设时返回 "{}"。</summary>
        public readonly Dictionary<string, Queue<string>> Responses =
            new Dictionary<string, Queue<string>>(StringComparer.OrdinalIgnoreCase);
        /// <summary>按动词的默认响应（不消费）。</summary>
        public readonly Dictionary<string, string> DefaultResponses =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Recorded Last => Requests.Count > 0 ? Requests[Requests.Count - 1] : null;
        public Recorded First(string verb)
        {
            foreach (var r in Requests) if (r.Method == verb) return r;
            return null;
        }

        public void RespondGet(string json) => Enqueue("GET", json);
        public void RespondPost(string json) => Enqueue("POST", json);
        public void RespondPut(string json) => Enqueue("PUT", json);
        public void Enqueue(string verb, string json)
        {
            if (!Responses.TryGetValue(verb, out var q)) { q = new Queue<string>(); Responses[verb] = q; }
            q.Enqueue(json);
        }
        public void SetDefault(string verb, string json) => DefaultResponses[verb] = json;

        private async Task<string> Record(string verb, string path, HttpContent content)
        {
            var r = new Recorded { Method = verb, Path = path };
            if (content != null)
            {
                try { r.ContentType = content.Headers?.ContentType?.ToString(); } catch { }
                if (content is ByteArrayContent bac)
                {
                    r.RawBody = await bac.ReadAsByteArrayAsync();
                    try { r.Body = System.Text.Encoding.UTF8.GetString(r.RawBody); } catch { }
                }
                else if (content is StringContent sc)
                {
                    r.Body = await sc.ReadAsStringAsync();
                    r.RawBody = System.Text.Encoding.UTF8.GetBytes(r.Body ?? "");
                }
                else
                {
                    try { r.Body = await content.ReadAsStringAsync(); } catch { r.Body = "<unreadable>"; }
                }
            }
            Requests.Add(r);
            if (Responses.TryGetValue(verb, out var q) && q.Count > 0) return q.Dequeue();
            return DefaultResponses.TryGetValue(verb, out var d) ? d : "{}";
        }

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
