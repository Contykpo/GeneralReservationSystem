using GeneralReservationSystem.Application.Entities.Authentication;

namespace GeneralReservationSystem.Application.DTOs.Authentication
{
    public class RegisterUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string UserNameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateUserDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
    }

    public class ChangePasswordDto
    {
        public int UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserKeyDto
    {
        public int UserId { get; set; }
    }

    public class UserInfo
    {
        public UserInfo() { }

        public UserInfo(User usr) 
        {
            UserId      = usr.UserId;
            UserName    = usr.UserName;
            Email       = usr.Email;
            IsAdmin     = usr.IsAdmin;
		}

        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
