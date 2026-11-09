using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VortexX.Services
{
    public enum AiProvider { Anthropic, OpenAI, Gemini, Custom }

    public class ChatAction
    {
        public JObject Json;
        public bool Applied;
    }

    public class ChatMsg
    {
        public string Role = "user";
        public string Text = "";
        public bool IsError;
        public List<ChatAction> Actions = new List<ChatAction>();
    }

    public static class AiService
    {
        const int TimeoutSeconds = 60;
        static readonly HttpClient Http = CreateHttp();

        static HttpClient CreateHttp()
        {
            var h = new HttpClient();
            h.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
            return h;
        }

        public static string DefaultModel(AiProvider p)
        {
            switch (p)
            {
                case AiProvider.Anthropic: return "claude-sonnet-5-5";
                case AiProvider.OpenAI: return "gpt-4o";
                case AiProvider.Gemini: return "gemini-3.8-flash";
                default: return "openai/gpt-4o-mini";
            }
        }

        public static string[] PresetModels(AiProvider p)
        {
            switch (p)
            {
                case AiProvider.Anthropic: return new string[] { "claude-sonnet-5-5", "claude-opus-5-5", "claude-haiku-5-5" };
                case AiProvider.OpenAI: return new string[] { "gpt-4o", "gpt-4o-mini" };
                case AiProvider.Gemini: return new string[] { "gemini-3.8-flash" };
                default: return new string[] { "openai/gpt-4o-mini" };
            }
        }

        public static string Protect(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            try
            {
                byte[] b = ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(b);
            }
            catch { return ""; }
        }

        public static string Unprotect(string enc)
        {
            if (string.IsNullOrEmpty(enc)) return "";
            try
            {
                byte[] b = ProtectedData.Unprotect(Convert.FromBase64String(enc), null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(b);
            }
            catch { return ""; }
        }

        static string CleanBase(string baseUrl)
        {
            string b = (baseUrl ?? "").Trim();
            while (b.EndsWith("/")) b = b.Substring(0, b.Length - 1);
            return b;
        }

        static string ErrMsg(string body)
        {
            try
            {
                JObject o = JObject.Parse(body);
                JToken e = o["error"];
                if (e != null)
                {
                    if (e.Type == JTokenType.String) return e.ToString();
                    JToken m = e["message"];
                    if (m != null) return m.ToString();
                }
            }
            catch { }
            return body.Length > 300 ? body.Substring(0, 300) : body;
        }

        static async Task<JObject> SendJson(HttpRequestMessage req)
        {
            string url = req.RequestUri.GetLeftPart(UriPartial.Path);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using (req)
                using (HttpResponseMessage resp = await Http.SendAsync(req))
                {
                    string body = await resp.Content.ReadAsStringAsync();
                    App.Logger.WriteLine("AiService", req.Method + " " + url + " -> " + (int)resp.StatusCode + " in " + sw.ElapsedMilliseconds + " ms");
                    if (!resp.IsSuccessStatusCode)
                        throw new Exception("HTTP " + (int)resp.StatusCode + ": " + ErrMsg(body));
                    return JObject.Parse(body);
                }
            }
            catch (TaskCanceledException)
            {
                App.Logger.WriteLine("AiService", req.Method + " " + url + " timed out after " + sw.ElapsedMilliseconds + " ms");
                throw new Exception(string.Format(Lang.T("chat.timeout"), TimeoutSeconds));
            }
        }

        static HttpRequestMessage Post(string url, JObject body)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
            return req;
        }

        public static async Task<string> ChatAsync(AiProvider p, string model, string key, string baseUrl, string system, List<ChatMsg> hist)
        {
            switch (p)
            {
                case AiProvider.Anthropic: return await ChatAnthropic(model, key, system, hist);
                case AiProvider.Gemini: return await ChatGemini(model, key, system, hist);
                case AiProvider.OpenAI: return await ChatOpenAI("https://api.openai.com/v1", model, key, system, hist);
                default: return await ChatOpenAI(CleanBase(baseUrl), model, key, system, hist);
            }
        }

        static async Task<string> ChatAnthropic(string model, string key, string system, List<ChatMsg> hist)
        {
            var msgs = new JArray();
            foreach (ChatMsg m in hist)
            {
                var o = new JObject();
                o["role"] = m.Role;
                o["content"] = m.Text;
                msgs.Add(o);
            }
            var body = new JObject();
            body["model"] = model;
            body["max_tokens"] = 2048;
            body["system"] = system;
            body["messages"] = msgs;

            HttpRequestMessage req = Post("https://api.anthropic.com/v1/messages", body);
            req.Headers.Add("x-api-key", key);
            req.Headers.Add("anthropic-version", "2023-06-01");
            JObject res = await SendJson(req);

            var sb = new StringBuilder();
            JArray content = res["content"] as JArray;
            if (content != null)
            {
                foreach (JToken c in content)
                {
                    if (c.Value<string>("type") == "text") sb.Append(c.Value<string>("text"));
                }
            }
            if (sb.Length == 0) throw new Exception("Empty response");
            return sb.ToString();
        }

        static async Task<string> ChatOpenAI(string baseUrl, string model, string key, string system, List<ChatMsg> hist)
        {
            var msgs = new JArray();
            var sys = new JObject();
            sys["role"] = "system";
            sys["content"] = system;
            msgs.Add(sys);
            foreach (ChatMsg m in hist)
            {
                var o = new JObject();
                o["role"] = m.Role;
                o["content"] = m.Text;
                msgs.Add(o);
            }
            var body = new JObject();
            body["model"] = model;
            body["messages"] = msgs;

            HttpRequestMessage req = Post(baseUrl + "/chat/completions", body);
            if (!string.IsNullOrEmpty(key)) req.Headers.Add("Authorization", "Bearer " + key);
            JObject res = await SendJson(req);

            JToken c = res.SelectToken("choices[0].message.content");
            string text = c == null ? "" : c.ToString();
            if (text.Length == 0) throw new Exception("Empty response");
            return text;
        }

        static async Task<string> ChatGemini(string model, string key, string system, List<ChatMsg> hist)
        {
            string mid = model.StartsWith("models/") ? model.Substring(7) : model;

            var contents = new JArray();
            foreach (ChatMsg m in hist)
            {
                var part = new JObject();
                part["text"] = m.Text;
                var o = new JObject();
                o["role"] = m.Role == "assistant" ? "model" : "user";
                o["parts"] = new JArray(part);
                contents.Add(o);
            }
            var sysPart = new JObject();
            sysPart["text"] = system;
            var sysInstr = new JObject();
            sysInstr["parts"] = new JArray(sysPart);

            var body = new JObject();
            body["systemInstruction"] = sysInstr;
            body["contents"] = contents;

            HttpRequestMessage req = Post("https://generativelanguage.googleapis.com/v1beta/models/" + mid + ":generateContent", body);
            req.Headers.Add("x-goog-api-key", key);
            JObject res = await SendJson(req);

            var sb = new StringBuilder();
            JArray parts = res.SelectToken("candidates[0].content.parts") as JArray;
            if (parts != null)
            {
                foreach (JToken t in parts)
                {
                    string s = t.Value<string>("text");
                    if (s != null) sb.Append(s);
                }
            }
            if (sb.Length == 0) throw new Exception("No response (the request may have been blocked)");
            return sb.ToString();
        }

        static bool SkipOpenAiModel(string id)
        {
            string l = id.ToLowerInvariant();
            string[] bad = { "embedding", "whisper", "tts", "dall-e", "moderation", "audio", "realtime", "transcribe", "image", "davinci", "babbage" };
            foreach (string b in bad) if (l.Contains(b)) return true;
            return false;
        }

        public static async Task<List<string>> ListModelsAsync(AiProvider p, string key, string baseUrl)
        {
            var ids = new List<string>();
            HttpRequestMessage req;
            switch (p)
            {
                case AiProvider.Anthropic:
                    req = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models?limit=100");
                    req.Headers.Add("x-api-key", key);
                    req.Headers.Add("anthropic-version", "2023-06-01");
                    break;
                case AiProvider.Gemini:
                    req = new HttpRequestMessage(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/models?pageSize=100");
                    req.Headers.Add("x-goog-api-key", key);
                    break;
                case AiProvider.OpenAI:
                    req = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
                    req.Headers.Add("Authorization", "Bearer " + key);
                    break;
                default:
                    req = new HttpRequestMessage(HttpMethod.Get, CleanBase(baseUrl) + "/models");
                    if (!string.IsNullOrEmpty(key)) req.Headers.Add("Authorization", "Bearer " + key);
                    break;
            }

            JObject o = await SendJson(req);

            if (p == AiProvider.Gemini)
            {
                JArray ms = o["models"] as JArray;
                if (ms != null)
                {
                    foreach (JToken m in ms)
                    {
                        bool ok = false;
                        JArray methods = m["supportedGenerationMethods"] as JArray;
                        if (methods != null)
                            foreach (JToken t in methods) if (t.ToString() == "generateContent") ok = true;
                        string n = m.Value<string>("name");
                        if (ok && n != null) ids.Add(n.StartsWith("models/") ? n.Substring(7) : n);
                    }
                }
            }
            else
            {
                JArray d = o["data"] as JArray;
                if (d != null)
                {
                    foreach (JToken m in d)
                    {
                        string id = m.Value<string>("id");
                        if (!string.IsNullOrEmpty(id)) ids.Add(id);
                    }
                }
            }

            if (p == AiProvider.OpenAI) ids = ids.Where(i => !SkipOpenAiModel(i)).ToList();
            if (p != AiProvider.Anthropic) ids.Sort(StringComparer.OrdinalIgnoreCase);
            if (ids.Count > 40) ids = ids.GetRange(0, 40);
            return ids;
        }

        static readonly Regex ActionRx = new Regex("```vortex-action\\s*(\\{.*?\\})\\s*```", RegexOptions.Singleline);

        public static string ExtractActions(string text, List<JObject> actions)
        {
            foreach (Match m in ActionRx.Matches(text))
            {
                try { actions.Add(JObject.Parse(m.Groups[1].Value)); }
                catch { }
            }
            return ActionRx.Replace(text, "").Trim();
        }
    }
}
