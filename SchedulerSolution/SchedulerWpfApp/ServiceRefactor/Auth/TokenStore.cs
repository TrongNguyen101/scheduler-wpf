using System;
using System.Security.Cryptography;
using System.Text;

namespace SchedulerWpfApp.ServiceRefactor.Auth
{
    public interface ITokenStore
    {
        void SaveRefreshToken(string token);
        string? GetRefreshToken();
        void Clear();
    }

    public class DpapiTokenStore : ITokenStore
    {
        private const string Entropy = "SchedulerWpfApp.RefreshTokenEntropy";
        private readonly string _scope = "SchedulerWpfApp.RefreshToken";

        public void SaveRefreshToken(string token)
        {
            var plaintext = Encoding.UTF8.GetBytes(token);
            var entropy = Encoding.UTF8.GetBytes(Entropy);
            var cipher = ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser);
            var b64 = Convert.ToBase64String(cipher);
            Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\SchedulerWpfApp", _scope, b64);
        }

        public string? GetRefreshToken()
        {
            var value = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\SchedulerWpfApp", _scope, null) as string;
            if (string.IsNullOrWhiteSpace(value)) return null;
            try
            {
                var cipher = Convert.FromBase64String(value);
                var entropy = Encoding.UTF8.GetBytes(Entropy);
                var plaintext = ProtectedData.Unprotect(cipher, entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plaintext);
            }
            catch { return null; }
        }

        public void Clear()
        {
            try { Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree("Software\\SchedulerWpfApp", throwOnMissingSubKey: false); } catch { }
        }
    }
}


