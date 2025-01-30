using System.Data;
using DotnetAPI.Data;
using DotnetAPI.Dtos;
using DotnetAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DotnetAPI.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class AuthController(IConfiguration config) : ControllerBase
{
    private readonly DataContextDapper _dapper = new(config);
    private readonly AuthHelper _authHelper = new(config);



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
                byte[] passwordSalt = _authHelper.GetPasswordSalt();
                byte[] passwordHash = _authHelper.GetPasswordHash(userForRegistrationDto.Password ?? "",
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
        byte[] passwordHash = _authHelper.GetPasswordHash(
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
        string token = _authHelper.CreateToken(userId);
        Dictionary<string, string> dict = [];
        dict.Add("userId", userId.ToString());
        dict.Add("email", userForLoginDto.Email ?? "");
        dict.Add("token", token);
        return Ok(dict);
    }

    [HttpGet("RefreshToken")]
    public IActionResult RefreshToken()
    {
        // Extracts data from our Headers from our claims array
        // Here we are extracting userId
        string userId = this.User.FindFirst("userId")?.Value ?? "";
        string sqlForUserId = @$"
                SELECT UserId FROM TutorialAppSchema.Users 
                    WHERE UserId = '{userId}'";
        int userIdFromDB = _dapper.LoadDataSingle<int>(sqlForUserId);
        string token = _authHelper.CreateToken(userIdFromDB);
        Dictionary<string, string> dict = [];
        dict.Add("token", token);
        return Ok(dict);
    }

}
