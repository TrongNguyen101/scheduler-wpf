using System;
using System.Text.Json;

namespace SchedulerWpfApp.ServiceRefactor.Auth
{
    public static class JwtParser
    {
        public static DateTimeOffset GetExpiryUtc(string jwt)
        {
            // JWT: header.payload.signature (base64url)
            var parts = jwt.Split('.');
            if (parts.Length < 2) return DateTimeOffset.MinValue;
            var payload = parts[1];
            string b64 = payload.Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4)
            {
                case 2: b64 += "=="; break;
                case 3: b64 += "="; break;
            }
            try
            {
                var bytes = Convert.FromBase64String(b64);
                var json = JsonDocument.Parse(bytes);
                if (json.RootElement.TryGetProperty("exp", out var expEl) && expEl.TryGetInt64(out long exp))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(exp);
                }
            }
            catch { }
            return DateTimeOffset.MinValue;
        }
    }
}


