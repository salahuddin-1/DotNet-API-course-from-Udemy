namespace DotnetAPI.Dtos
{
    public partial class UserForRegistrationSpDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string PasswordConfirm { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Gender { get; set; }
        public required string JobTitle { get; set; }
        public required string Department { get; set; }
        public required decimal Salary { get; set; }
    }
}