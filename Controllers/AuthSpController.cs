using System.Data;
using Dapper;
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
public class AuthSpController(IConfiguration config) : ControllerBase
{
    private readonly DataContextDapper _dapper = new(config);
    private readonly AuthHelper _authHelper = new(config);



    [AllowAnonymous]
    [HttpPost("Register")]
    public IActionResult Register(UserForRegistrationSpDto userForRegistrationDto)
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
                List<SqlParameter> sqlParameters = [];
                // EMAIL PARAM
                SqlParameter emailParameter = new SqlParameter(
                   "@EmailParam",
                   SqlDbType.VarChar
               );
                emailParameter.Value = userForRegistrationDto.Email;
                sqlParameters.Add(emailParameter);
                // PASSWORD SALT PARAM
                SqlParameter passwordSaltParameter = new SqlParameter(
                   "@PasswordSaltParam",
                   SqlDbType.VarBinary
               );
                passwordSaltParameter.Value = passwordSalt;
                sqlParameters.Add(passwordSaltParameter);
                // PASSWORD HASH PARAM
                SqlParameter passwordHashParameter = new SqlParameter(
                    "@PasswordHashParam",
                    SqlDbType.VarBinary
                );
                passwordHashParameter.Value = passwordHash;
                sqlParameters.Add(passwordHashParameter);
                string sqlAddAuth = @"
                    EXEC TutorialAppSchema.spRegistration_Upsert
                        @Email = @EmailParam,
                        @PasswordSalt = @PasswordSaltParam,
                        @PasswordHash = @PasswordHashParam";
                if (_dapper.ExecuteSqlWithParameters(sql: sqlAddAuth, parameters: sqlParameters))
                {
                    byte ACTIVE = 1;
                    string sqlAddUser = @$"EXEC TutorialAppSchema.spUsers_Upsert
                                            @FirstName='{userForRegistrationDto.FirstName}', 
                                            @LastName='{userForRegistrationDto.LastName}', 
                                            @Gender='{userForRegistrationDto.Gender}', 
                                            @Email='{userForRegistrationDto.Email}',
                                            @Active={ACTIVE},
                                            @JobTitle='{userForRegistrationDto.JobTitle}',
                                            @Department='{userForRegistrationDto.Department}',
                                            @Salary={userForRegistrationDto.Salary}";
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
        DynamicParameters sqlParameters = new();
        sqlParameters.Add(
            "@EmailParam",
            userForLoginDto.Email,
            DbType.String
        );
        string sql = $@"
        EXEC TutorialAppSchema.spLoginConfirmation_Get 
            @Email = @EmailParam";
        UserForLoginConfirmationDto? userForConfirmation = _dapper
            .LoadDataSingleWithParameters<UserForLoginConfirmationDto>(
                sql,
                parameters: sqlParameters
            );
        byte[] passwordHash = _authHelper.GetPasswordHash(
             password: userForLoginDto.Password ?? "",
             passwordSalt: userForConfirmation?.PasswordSalt ?? []
         );
        for (int i = 0; i < passwordHash.Length; i++)
        {
            if (passwordHash[i] != userForConfirmation?.PasswordHash![i])
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
