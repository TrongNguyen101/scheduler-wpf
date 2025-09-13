using System.Collections.Generic;

namespace SchedulerWpfApp.ServiceRefactor.Auth
{
    public class LoginRequest
    {
        public string username { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string accessToken { get; set; } = string.Empty;
        public string refreshToken { get; set; } = string.Empty;
        public UserDto user { get; set; } = new UserDto();
    }

    public class AccessTokenResponse
    {
        public string accessToken { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public string id { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public List<string> roles { get; set; } = new List<string>();
    }
}


