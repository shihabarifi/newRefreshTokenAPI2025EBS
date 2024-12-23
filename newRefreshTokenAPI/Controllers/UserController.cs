using Microsoft.AspNetCore.Mvc;
using newRefreshTokenAPI.Models;
using Microsoft.EntityFrameworkCore;
using newRefreshTokenAPI.Services;
using newRefreshTokenAPI.DTO;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace newRefreshTokenAPI.Controllers
{

    [ApiController]
    [Route("api/user/")]
    public class UserController : ControllerBase
    {
        private UserContext _context;
        private readonly IPasswordHash _passwordHashService;
        private readonly IGenerateToken _tokenService;

        public UserController(UserContext context, IPasswordHash passwordHashService, IGenerateToken tokenService)
        {
            _context = context;
            _passwordHashService = passwordHashService;
            _tokenService = tokenService;
        }

        public User _user = new User();

        [HttpPost("register")]
        [AllowAnonymous]

        //Action method .First checks whether username already exists>If not saves the user in DB along with hashed password
        public async Task<ActionResult<User>> Register(UserRequest request)
        {
            var exisitingUser = await _context.Users!.FirstOrDefaultAsync(user => user.username == request.username);

            if (exisitingUser != null)
            {
                return BadRequest("Username already exists");
            }
            else
            {
                _passwordHashService.HashPassword(request.password!, out byte[] passwordHash, out byte[] passwordSalt);

                _user.username = request.username;
                _user.passwordHash = passwordHash;
                _user.passwordSalt = passwordSalt;
                _user.role = request.role;
                _user.email = request.mail;
                _context.Users!.Add(_user);
                await _context.SaveChangesAsync();
                return Ok(_user);

            }
        }

        [AllowAnonymous]

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginRequest request)
        {

            var exisitingUser = await _context.Users!.FirstOrDefaultAsync(user => user.username == request.username);

            if (exisitingUser == null)
            {
                return BadRequest("User not found");
            }
            else if (!_passwordHashService.VerifyPasssword(request.password, exisitingUser.passwordHash, exisitingUser.passwordSalt))
            {
                return BadRequest("Incorrect password");
            }
            else
            {
                var authModel = new AuthModel();
                var userToken = _tokenService.GenerateAccessToken(exisitingUser);
                var refreshToken = _tokenService.GenerateRefreshToken();
                await _tokenService.SetRefreshToken(refreshToken, Response);

                exisitingUser.refreshToken = refreshToken.Token;
                exisitingUser.dateCreated = refreshToken.Created;
                exisitingUser.tokenExpires = refreshToken.Expires;

                authModel.IsAuthenticated = true;
                authModel.Token = userToken;
                authModel.Email = exisitingUser.email;
                authModel.Username = exisitingUser.username;
                authModel.Roles = exisitingUser.role;
                authModel.Message = "Success";
               // exisitingUser.refreshToken
                authModel.RefreshToken = refreshToken.Token;
                authModel.RefreshTokenExpiration = refreshToken.Expires;


                await _context.SaveChangesAsync();
                return Ok(authModel);
            }

        }


        [HttpGet("protected")]
        [Authorize]
        public string ProtectedAPI()
        {

            return "Protected Resource.You are authorized to access this resource";
        }

        [HttpGet("GetCurrncies")]
        [Authorize]
        public string GetCurrnciesAPI()
        {


            return "Protected Resource.You are authorized to access this resource";
        }


        [HttpGet("weather")]
        [Authorize]
        public ActionResult<WeatherForecast[]> GetWeatherForecast()
        {
            var startDate = DateOnly.FromDateTime(DateTime.Now);
            var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
            return Ok( Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = summaries[Random.Shared.Next(summaries.Length)]
            }).ToArray());
        }
        
    
        //[HttpPost("refresh-token")]
        //public async Task<ActionResult<string>> RefreshToken()
        //{
            [HttpPost]
            [Route("refresh-token")]
            public async Task<IActionResult> RefreshToken(CustomUserClaims userSession)
        {
            
           var refreshToken = Request.Cookies["refreshToken"];
            var existinguser = await _context.Users!.FirstOrDefaultAsync(user => user.username == userSession.UserName);


           // var existinguser = await _context.Users!.FirstOrDefaultAsync(user => user.refreshToken == existinguser1.refreshToken);


            if (existinguser == null)
            {
                return Unauthorized("Invalid Refresh Token.");
            }
            else if (existinguser.tokenExpires < DateTime.Now)
            {
                return Unauthorized("Token Expired");
            }
            else
            {
                string token = _tokenService.GenerateAccessToken(existinguser);
                var newRefreshToken = _tokenService.GenerateRefreshToken();
                existinguser.refreshToken = newRefreshToken.Token;
                existinguser.dateCreated = newRefreshToken.Created;
                existinguser.tokenExpires = newRefreshToken.Expires;
                await _context.SaveChangesAsync();
                await _tokenService.SetRefreshToken(newRefreshToken, Response);
                return Ok(new
                {
                    message = "Success",
                    JWTToken = token,
                    Flag = true,
                    refreshToken = newRefreshToken.Token,
                    Expires = newRefreshToken.Expires
                });
                //return Ok(token);
            }
        }

        //////////////////////
        ///
        [HttpPost]
        [Route("refresh-token1")]
        public async Task<IActionResult> RefreshToken1()
        {

            var refreshToken = Request.Cookies["refreshToken"];



            var existinguser = await _context.Users!.FirstOrDefaultAsync(user => user.refreshToken == refreshToken);
                

            if (existinguser == null)
            {
                return Unauthorized("Invalid Refresh Token.");
            }
            else if (existinguser.tokenExpires < DateTime.Now)
            {
                return Unauthorized("Token Expired");
            }
            else
            {
                string token = _tokenService.GenerateAccessToken(existinguser);
                var newRefreshToken = _tokenService.GenerateRefreshToken();
                existinguser.refreshToken = newRefreshToken.Token;
                existinguser.dateCreated = newRefreshToken.Created;
                existinguser.tokenExpires = newRefreshToken.Expires;
                await _context.SaveChangesAsync();
                await _tokenService.SetRefreshToken(newRefreshToken, Response);
                return Ok(token);
            }
        }

    }
}