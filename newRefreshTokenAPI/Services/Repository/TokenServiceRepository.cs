using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using newRefreshTokenAPI.Models;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace newRefreshTokenAPI.Services
{
    public class TokenServices : IGenerateToken
    {
        private readonly IConfiguration _configure;
        private readonly IConfiguration config;
        public TokenServices(IConfiguration configure)
        {
            _configure = configure;
            this.config = config;
        }

        //Service to generate Access Token
        public string GenerateAccessToken(User user)
        {
            var securityKey =new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configure.GetSection("JWT:SecretKey").Value));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.Name,user.username!),
                new Claim(ClaimTypes.Role, user.role!)
            };
            var token = new JwtSecurityToken(
                issuer: _configure["JWT:Issuer"],
                audience: _configure["JWT:Audience"],
                claims: userClaims,
                expires: DateTime.UtcNow.AddSeconds(10),
                signingCredentials: credentials

                );
            return new JwtSecurityTokenHandler().WriteToken(token);
            //    List<Claim> claims = new List<Claim>
            //    {
            //        new Claim(ClaimTypes.Name,user.username),
            //    new Claim("role", user.role)
            //};
            //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configure.GetSection("JWT:SecretKey").Value));

            //    var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            //    var token = new JwtSecurityToken(
            //        claims: claims,
            //          issuer: _configure["JWT:Issuer"],
            //        audience: _configure["JWT:Audience"],
            //        expires: DateTime.Now.AddMinutes(1),
            //        signingCredentials: cred);

            //    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            //    return jwt;
        }

        //Service to generate refresh Token
        public RefreshToken GenerateRefreshToken()
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.Now.AddMinutes(2),
                Created = DateTime.Now
            };
            return refreshToken;
        }

        //Service to set refresh token in response headers
        public async Task SetRefreshToken(RefreshToken newRefreshToken, HttpResponse Response)
        {

            var cookie = new CookieOptions
            {
                HttpOnly = true,
                Expires = newRefreshToken.Expires
            };
            Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookie);
        }
    }
}