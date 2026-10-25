using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChessCore.Models;

namespace ChessCore.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
    }

    public class TokenPayload
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public int Elo { get; set; }
        public long ExpiresAtUnix { get; set; }
    }

    public class TokenService : ITokenService
    {
        private static readonly byte[] SecretKey = Encoding.UTF8.GetBytes("ChessCore_Secret_Key_For_Jwt_Bearer_2026_HK7_KLCN_153!");

        public string GenerateToken(User user)
        {
            var payload = new TokenPayload
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email ?? string.Empty,
                Role = user.Role ?? "Player",
                PlayerId = user.PlayerId ?? string.Empty,
                Elo = user.Elo,
                ExpiresAtUnix = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
            };

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            var payloadBase64 = Convert.ToBase64String(jsonBytes);

            using var hmac = new HMACSHA256(SecretKey);
            var sigBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
            var sigBase64 = Convert.ToBase64String(sigBytes);

            return $"{payloadBase64}.{sigBase64}";
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var parts = token.Split('.');
            if (parts.Length != 2) return null;

            var payloadBase64 = parts[0];
            var sigBase64 = parts[1];

            try
            {
                using var hmac = new HMACSHA256(SecretKey);
                var expectedSig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
                var actualSig = Convert.FromBase64String(sigBase64);

                if (!CryptographicOperations.FixedTimeEquals(expectedSig, actualSig))
                {
                    return null;
                }

                var jsonBytes = Convert.FromBase64String(payloadBase64);
                var payload = JsonSerializer.Deserialize<TokenPayload>(jsonBytes);
                if (payload == null) return null;

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.ExpiresAtUnix)
                {
                    return null; // Expired
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, payload.UserId.ToString()),
                    new Claim(ClaimTypes.Name, payload.Username),
                    new Claim(ClaimTypes.Email, payload.Email),
                    new Claim(ClaimTypes.Role, payload.Role),
                    new Claim("PlayerId", payload.PlayerId),
                    new Claim("Elo", payload.Elo.ToString())
                };

                var identity = new ClaimsIdentity(claims, "Bearer");
                return new ClaimsPrincipal(identity);
            }
            catch
            {
                return null;
            }
        }
    }
}
