using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;

namespace DotnetAPI.Helpers;

public class AuthHelper(IConfiguration config)
{
    private readonly IConfiguration _config = config;

    public byte[] GetPasswordHash(string password, byte[] passwordSalt)
    {
        string passwordSaltToBase64 = Convert.ToBase64String(passwordSalt);
        // [passwordSaltPlusPassKeyFromAppSettings] is a string not a base64 string
        // it is getting concatenated with a base64 string
        string? passWordKey = _config.GetSection("AppSettings:PasswordKey").Value;
        string? passwordSaltPlusPassKeyFromAppSettings = passWordKey + passwordSaltToBase64;

        // Converting the string to bytes[], remember here we are not using the method
        // Convert.FromBase64String(passwordSaltPlusPassKeyFromAppSettings), cuz
        // our string is not base64 but a string concatenated with a base64 thus a normal string
        byte[] finalSalt = Encoding.ASCII.GetBytes(passwordSaltPlusPassKeyFromAppSettings);
        byte[] passwordHash = KeyDerivation.Pbkdf2(
             password: password,
             salt: finalSalt,
             prf: KeyDerivationPrf.HMACSHA256,
             iterationCount: 1000000,
             numBytesRequested: 256 / 8
         );
        return passwordHash;
    }

    public byte[] GetPasswordSalt()
    {
        byte[] passwordSalt = new byte[128 / 8];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        // Populate the byte[] array with random non-zero bytes (from 1 - 255)
        rng.GetNonZeroBytes(passwordSalt);
        return passwordSalt;
    }

    public string CreateToken(int userId)
    {
        string tokenKeyFromAppSettings = _config
            .GetSection("AppSettings:TokenKey")
            .Value!;
        byte[] tokenKeyFromAppSettingsInBytes = Encoding
            .UTF8
            .GetBytes(tokenKeyFromAppSettings);
        SymmetricSecurityKey tokenKey = new(tokenKeyFromAppSettingsInBytes);
        SigningCredentials credentials = new(
            tokenKey,
            SecurityAlgorithms.HmacSha256Signature
        );
        Claim[] claims = [
            new("userId", userId.ToString())
        ];
        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = credentials,
            Expires = DateTime.Now.AddDays(1)
        };
        JwtSecurityTokenHandler tokenHandler = new();
        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        string tokenInString = tokenHandler.WriteToken(token);
        return tokenInString;
    }
}
