using System;
using System.Collections.Generic;

namespace GeoServerDesktop.Tests.Infrastructure
{
    public enum CheckStatus { Pass, Warn, Fail }

    public sealed class CheckResult
    {
        public string Name;
        public CheckStatus Status;
        public string Message;
        public override string ToString() => $"[{Status}] {Name} :: {Message}";
    }

    /// <summary>
    /// 双形态断言：控制台 harness 收集 CheckResult 并统计，xunit 侧用 ThrowOnFail 转为断言失败。
    /// 期望值必须由数据文件头/确定性公式独立推导，禁止从被测系统回抄。
    /// </summary>
    public static class Check
    {
        public readonly static List<CheckResult> All = new List<CheckResult>();
        private static readonly object Gate = new object();

        public static CheckResult Pass(string name, string msg = "ok") => Add(name, CheckStatus.Pass, msg);
        public static CheckResult Warn(string name, string msg) => Add(name, CheckStatus.Warn, msg);
        public static CheckResult Fail(string name, string msg) => Add(name, CheckStatus.Fail, msg);

        public static CheckResult Cond(bool cond, string name, string passMsg, string failMsg, bool warnInsteadOfFail = false)
        {
            if (cond) return Pass(name, passMsg);
            return warnInsteadOfFail ? Warn(name, failMsg) : Fail(name, failMsg);
        }

        private static CheckResult Add(string name, CheckStatus s, string msg)
        {
            var r = new CheckResult { Name = name, Status = s, Message = msg };
            lock (Gate) All.Add(r);
            return r;
        }

        /// <summary>xunit 侧使用：若 Fail 则抛出异常。</summary>
        public static void ThrowOnFail(params CheckResult[] results)
        {
            foreach (var r in results)
                if (r.Status == CheckStatus.Fail)
                    throw new Exception("检查失败: " + r.Name + " :: " + r.Message);
        }

        public static (int pass, int warn, int fail) Summarize(IEnumerable<CheckResult> results = null)
        {
            lock (Gate)
            {
                int p = 0, w = 0, f = 0;
                foreach (var r in (results ?? All))
                {
                    if (r.Status == CheckStatus.Pass) p++;
                    else if (r.Status == CheckStatus.Warn) w++;
                    else f++;
                }
                return (p, w, f);
            }
        }

        public static void Reset() { lock (Gate) All.Clear(); }
    }
}
