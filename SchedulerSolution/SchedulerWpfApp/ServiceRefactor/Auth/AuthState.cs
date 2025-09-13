using System;
using System.Collections.Generic;
using System.Linq;

namespace SchedulerWpfApp.ServiceRefactor.Auth
{
    public class AuthState
    {
        public bool IsAuthenticated { get; private set; }
        public UserDto CurrentUser { get; private set; } = new UserDto();
        public string AccessToken { get; private set; } = string.Empty;
        public string RefreshToken { get; private set; } = string.Empty;
        public DateTimeOffset AccessTokenExpiryUtc { get; private set; } = DateTimeOffset.MinValue;

        public event Action? Changed;

        public void SetSession(string accessToken, string refreshToken, UserDto user, DateTimeOffset accessExp)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            CurrentUser = user;
            AccessTokenExpiryUtc = accessExp;
            IsAuthenticated = true;
            Changed?.Invoke();
        }

        public void Clear()
        {
            AccessToken = string.Empty;
            RefreshToken = string.Empty;
            CurrentUser = new UserDto();
            AccessTokenExpiryUtc = DateTimeOffset.MinValue;
            IsAuthenticated = false;
            Changed?.Invoke();
        }

        public bool HasRole(params string[] roles)
        {
            if (!IsAuthenticated || CurrentUser.roles == null) return false;
            return roles.Any(r => CurrentUser.roles.Contains(r));
        }
    }
}


