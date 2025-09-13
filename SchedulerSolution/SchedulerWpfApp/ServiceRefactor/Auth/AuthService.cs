using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SchedulerWpfApp.ServiceRefactor.Auth
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly AuthState _state;
        private readonly ITokenStore _tokenStore;
        private readonly AuthConfig _config;

        public AuthService(HttpClient http, AuthState state, ITokenStore tokenStore, AuthConfig config)
        {
            _http = http;
            _state = state;
            _tokenStore = tokenStore;
            _config = config;
            _http.BaseAddress = new Uri(config.BaseUrl);
        }

        private static StringContent JsonBody(object o) => new StringContent(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

        public async Task<bool> TrySilentLoginAsync()
        {
            var refresh = _tokenStore.GetRefreshToken();
            if (string.IsNullOrWhiteSpace(refresh)) return false;
            try
            {
                var res = await _http.PostAsync("/auth/refresh", JsonBody(new { refreshToken = refresh }));
                if (!res.IsSuccessStatusCode) return false;
                var data = JsonSerializer.Deserialize<AccessTokenResponse>(await res.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                var meReq = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
                meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", data.accessToken);
                var meRes = await _http.SendAsync(meReq);
                if (!meRes.IsSuccessStatusCode) return false;
                var me = JsonSerializer.Deserialize<UserDto>(await meRes.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                var exp = JwtParser.GetExpiryUtc(data.accessToken);
                _state.SetSession(data.accessToken, refresh, me, exp);
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var res = await _http.PostAsync("/auth/login", JsonBody(new LoginRequest { username = username, password = password }));
            if (!res.IsSuccessStatusCode) return false;
            var data = JsonSerializer.Deserialize<LoginResponse>(await res.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var exp = JwtParser.GetExpiryUtc(data.accessToken);
            _tokenStore.SaveRefreshToken(data.refreshToken);
            _state.SetSession(data.accessToken, data.refreshToken, data.user, exp);
            return true;
        }

        public async Task<bool> RefreshAsync()
        {
            if (!_state.IsAuthenticated) return false;
            var refresh = _state.RefreshToken;
            var res = await _http.PostAsync("/auth/refresh", JsonBody(new { refreshToken = refresh }));
            if (!res.IsSuccessStatusCode) return false;
            var data = JsonSerializer.Deserialize<AccessTokenResponse>(await res.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var exp = JwtParser.GetExpiryUtc(data.accessToken);
            _state.SetSession(data.accessToken, refresh, _state.CurrentUser, exp);
            return true;
        }

        public void Logout()
        {
            _tokenStore.Clear();
            _state.Clear();
        }
    }
}


