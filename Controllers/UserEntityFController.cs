using AutoMapper;
using DotnetAPI.Data;
using DotnetAPI.Dtos;
using DotnetAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserEntityFController : ControllerBase
    {

        private IMapper _mapper;
        private IUserRepository _userRepository;

        public UserEntityFController(IConfiguration config, IUserRepository userRepository)
        {
            MapperConfiguration mapperConfiguration = new(
                    cfg =>
                    {
                        cfg.CreateMap<UserToAddDto, User>();
                    }
                );
            _mapper = new Mapper(mapperConfiguration);
            _userRepository = userRepository;
        }


        [HttpGet("GetUsers")]
        public IEnumerable<User> GetUsers()
        {
            IEnumerable<User> users = _userRepository.GetUsers();
            return users;
        }


        [HttpGet("GetSingleUser/{userId}")]
        public User GetSingleUser(int userId)
        {
            return _userRepository.GetSingleUser(userId: userId);
        }

        [HttpPut("EditUser")]
        public IActionResult EditUser(User user)
        {
            User? userDb = _userRepository.GetSingleUser(userId: user.UserId);
            if (userDb != null)
            {
                userDb.Active = user.Active;
                userDb.FirstName = user.FirstName;
                userDb.LastName = user.LastName;
                userDb.Email = user.Email;
                userDb.Gender = user.Gender;
                if (_userRepository.SaveChanges())
                {
                    return Ok();
                }
                throw new Exception("Failed to update user");

            }
            throw new Exception("Failed to get user");
        }

        [HttpPost("AddUser")]
        public IActionResult AddUser(UserToAddDto user)
        {
            User userDb = _mapper.Map<User>(user);
            _userRepository.AddEntity<User>(userDb);
            if (_userRepository.SaveChanges())
            {
                return Ok();
            }
            throw new Exception("Failed to add user");

        }

        [HttpDelete("DeleteUser/{userId}")]
        public IActionResult DeleteUser(int userId)
        {
            User user = _userRepository.GetSingleUser(userId: userId);
            _userRepository.RemoveEntity<User>(entityToAdd: user);
            if (_userRepository.SaveChanges())
            {
                return Ok();
            }
            throw new Exception("Failed to delete user");

        }
    }
}


