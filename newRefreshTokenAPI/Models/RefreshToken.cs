using System;
using System.Text.Json.Serialization;

namespace newRefreshTokenAPI.Models
{
    public class RefreshToken
    {
        public string? Token {get;set;}
        public string? JWTToken { get;set; }
        public DateTime Created {get;set;}
        public DateTime Expires {get;set;}
    }
    public class AuthModel
    {
        public string? Message { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Roles { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiresOn { get; set; }

        [JsonIgnore]
        public string? RefreshToken { get; set; }

        public DateTime RefreshTokenExpiration { get; set; }
    }
}