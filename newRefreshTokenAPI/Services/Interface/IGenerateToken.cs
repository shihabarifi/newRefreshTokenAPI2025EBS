using newRefreshTokenAPI.Models;
using newRefreshTokenAPI.Responses;

namespace newRefreshTokenAPI.Services
{
    public interface IGenerateToken
    {
        public string GenerateAccessToken(User user);
        public RefreshToken GenerateRefreshToken();
       // LoginResponses RefreshToken(UserSession userSession);
        public Task SetRefreshToken(RefreshToken newRefreshToken, HttpResponse Response);
    }
}