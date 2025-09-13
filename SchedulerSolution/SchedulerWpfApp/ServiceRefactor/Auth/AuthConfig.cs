using System;

namespace SchedulerWpfApp.ServiceRefactor.Auth
{
    public class AuthConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:4000";
        public int AutoRefreshBeforeExpirySeconds { get; set; } = 60;
        public string TokenStorage { get; set; } = "DPAPI"; // DPAPI | CredentialManager | Memory
    }
}


