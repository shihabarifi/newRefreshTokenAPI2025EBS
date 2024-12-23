namespace newRefreshTokenAPI.Responses
{
    public class CustomResponses
    {
        public record RegisterResponses(bool Flag = false, string Message = null!);
        public record LoginResponses(bool Flag = false, string Message = null!, string JWTToken = null!);

    }
}
