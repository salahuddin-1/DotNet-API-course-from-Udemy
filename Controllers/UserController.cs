using System.Data;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Dtos;
using DotnetAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {

        private DataContextDapper _dapper;

        public UserController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("TestConnection")]
        public DateTime TestConnection()
        {
            return _dapper.LoadDataSingle<DateTime>(
                "SELECT GETDATE()"
                );
        }

        [HttpGet("GetUsers")]
        public IEnumerable<User> GetUsers()
        {
            string sql = @"
                SELECT [UserId],
                    [FirstName],
                    [LastName],
                    [Email],
                    [Gender],
                    [Active] 
                FROM TutorialAppSchema.Users";
            IEnumerable<User> users = _dapper.LoadData<User>(sql);
            return users;
        }


        [HttpGet("GetSingleUser/{userId}")]
        public User GetSingleUser(int userId)
        {
            string sql = @"
                SELECT [UserId],
                    [FirstName],
                    [LastName],
                    [Email],
                    [Gender],
                    [Active] FROM TutorialAppSchema.Users
                WHERE UserId = " + userId.ToString();
            User user = _dapper.LoadDataSingle<User>(sql);
            return user;
        }

        [HttpPut("EditUser")]
        public IActionResult EditUser(User user)
        {
            Console.WriteLine("Called");
            string sql = @"
                UPDATE TutorialAppSchema.Users
                SET [FirstName] = '" + user.FirstName +
                "', [LastName] = '" + user.LastName +
                "', [Email] =  '" + user.Email +
                "', [Gender] =  '" + user.Gender +
                "',  [Active] =  '" + user.Active +
                "' WHERE UserId = " + user.UserId;

            Console.WriteLine(sql);

            if (_dapper.ExecuteSql(sql))
            {

                return Ok();
            }
            throw new Exception("Failed to update user");
        }

        [HttpPost("AddUser")]
        public IActionResult AddUser(UserToAddDto user)
        {
            string sql = @"
                INSERT INTO TutorialAppSchema.Users(
                    [FirstName],
                    [LastName],
                    [Email],
                    [Gender],
                    [Active]
                ) VALUES (" +
                    "'" + user.FirstName +
                    "', '" + user.LastName +
                    "', '" + user.Email +
                    "', '" + user.Gender +
                    "', '" + user.Active +
                "')";

            Console.WriteLine(sql);

            if (_dapper.ExecuteSql(sql))
            {

                return Ok();
            }
            throw new Exception("Failed to add user");
        }

        [HttpPost("AddUserWithDynamicParameters")]
        public IActionResult AddUserWithDynamicParameters(UserToAddDto user)
        {
            string sql = @"
                INSERT INTO TutorialAppSchema.Users(
                    [FirstName],
                    [LastName],
                    [Email],
                    [Gender],
                    [Active]
                ) VALUES (
                    @FirstNameParam,
                    @LastNameParam,
                    @EmailParam,
                    @GenderParam,
                    @ActiveParam
                )";
            DynamicParameters sqlParameters = new();
            sqlParameters.Add("@FirstNameParam", user.FirstName, DbType.String);
            sqlParameters.Add("@LastNameParam", user.LastName, DbType.String);
            sqlParameters.Add("@EmailParam", user.Email, DbType.String);
            sqlParameters.Add("@GenderParam", user.Gender, DbType.String);
            sqlParameters.Add("@ActiveParam", user.Active, DbType.Boolean);
            if (_dapper.ExecuteSqlWithDynamicParameters(sql, sqlParameters))
            {
                return Ok();
            }
            throw new Exception("Failed to add user");
        }

        [HttpDelete("DeleteUser/{userId}")]
        public IActionResult DeleteUser(int userId)
        {
            string sql = @"
                DELETE FROM TutorialAppSchema.Users 
                WHERE UserId = " + userId.ToString();

            Console.WriteLine(sql);

            if (_dapper.ExecuteSql(sql))
            {
                return Ok();
            }
            throw new Exception("Failed to delete user");
        }
    }
}


