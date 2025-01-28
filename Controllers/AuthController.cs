using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DotnetAPI.Data;
using DotnetAPI.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace DotnetAPI.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class AuthController(IConfiguration config) : ControllerBase
{
    private readonly DataContextDapper _dapper = new(config);
    private readonly IConfiguration _config = config;

    [AllowAnonymous]
    [HttpPost("Register")]
    public IActionResult Register(UserForRegistrationDto userForRegistrationDto)
    {
        if (userForRegistrationDto.Password == userForRegistrationDto.PasswordConfirm)
        {
            string sqlCheckUserExists =
                "SELECT Email FROM TutorialAppSchema.Auth WHERE Email = '"
                + userForRegistrationDto.Email
                + "'";
            IEnumerable<string> existingUsers = _dapper.LoadData<string>(sqlCheckUserExists);
            if (existingUsers.Count() == 0)
            {
                byte[] passwordSalt = GetPasswordSalt();
                byte[] passwordHash = GetPasswordHash(userForRegistrationDto.Password ?? "",
                                         passwordSalt: passwordSalt);
                string sqlEmail = "'" + userForRegistrationDto.Email + "'";
                string sqlAddAuth = @"
                    INSERT INTO TutorialAppSchema.Auth(
                        [Email],
                        [PasswordHash],
                        [PasswordSalt]
                        ) VALUES(" + sqlEmail + ", @PasswordHash, @PasswordSalt)";

                List<SqlParameter> sqlParameters = [];
                SqlParameter passwordSaltParameter = new SqlParameter(
                   "@PasswordSalt",
                   SqlDbType.VarBinary
               );
                passwordSaltParameter.Value = passwordSalt;
                SqlParameter passwordHashParameter = new SqlParameter(
                    "@PasswordHash",
                    SqlDbType.VarBinary
                );
                passwordHashParameter.Value = passwordHash;
                sqlParameters.Add(passwordHashParameter);
                sqlParameters.Add(passwordSaltParameter);
                if (_dapper.ExecuteSqlWithParameters(sql: sqlAddAuth, parameters: sqlParameters))
                {
                    byte ACTIVE = 1;
                    string sqlAddUser = @$"INSERT INTO TutorialAppSchema.Users(
                                        [FirstName],
                                        [LastName],
                                        [Email],
                                        [Gender],
                                        [Active]
                                    ) VALUES (
                                        '{userForRegistrationDto.FirstName}',
                                        '{userForRegistrationDto.LastName}',
                                        '{userForRegistrationDto.Email}',
                                        '{userForRegistrationDto.Gender}',
                                        {ACTIVE}
                                    )";
                    if (_dapper.ExecuteSql(sqlAddUser))
                    {
                        return Ok();
                    }
                    throw new Exception("Failed to add user");
                }
                throw new Exception("Failed to register user");
            }
            throw new Exception("User with this email already exists");
        }
        throw new Exception("Passwords do not match");
    }


    [AllowAnonymous]
    [HttpPost("Login")]
    public IActionResult Login(UserForLoginDto userForLoginDto)
    {

        string sqlForHashAndSalt = $@"
                SELECT [PasswordHash],
                    [PasswordSalt] 
                    FROM TutorialAppSchema.Auth
                    WHERE Email = '{userForLoginDto.Email}'";
        UserForLoginConfirmationDto userForConfirmation =
            _dapper
            .LoadDataSingle<UserForLoginConfirmationDto>(sqlForHashAndSalt);
        byte[] passwordHash = GetPasswordHash(
             password: userForLoginDto.Password ?? "",
             passwordSalt: userForConfirmation.PasswordSalt ?? []
         );
        for (int i = 0; i < passwordHash.Length; i++)
        {
            if (passwordHash[i] != userForConfirmation.PasswordHash![i])
            {
                return StatusCode(401, "Incorrect password");
            }
        }
        string sqlForUserId = @$"
                SELECT UserId FROM TutorialAppSchema.Users 
                    WHERE Email = '{userForLoginDto.Email}'";
        int userId = _dapper.LoadDataSingle<int>(sql: sqlForUserId);
        string token = CreateToken(userId);
        Dictionary<string, string> dict = [];
        dict.Add("token", token);
        return Ok(dict);
    }

    [HttpGet("RefreshToken")]
    public IActionResult RefreshToken()
    {
        // Extracts data from our Headers from our claims array
        // Here we are extracting userId
        string userId = User.FindFirst("userId")?.Value ?? "";
        string sqlForUserId = @$"
                SELECT UserId FROM TutorialAppSchema.Users 
                    WHERE UserId = '{userId}'";
        int userIdFromDB = _dapper.LoadDataSingle<int>(sqlForUserId);
        string token = CreateToken(userIdFromDB);
        Dictionary<string, string> dict = [];
        dict.Add("token", token);
        return Ok(dict);
    }
    private byte[] GetPasswordHash(string password, byte[] passwordSalt)
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

    private byte[] GetPasswordSalt()
    {
        byte[] passwordSalt = new byte[128 / 8];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        // Populate the byte[] array with random non-zero bytes (from 1 - 255)
        rng.GetNonZeroBytes(passwordSalt);
        return passwordSalt;
    }

    private string CreateToken(int userId)
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
