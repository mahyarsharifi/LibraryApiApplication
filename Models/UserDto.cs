﻿using Entities;
using System.ComponentModel.DataAnnotations;
using WebFramework.Api;

namespace LibraryApplication_Api.Models
{
    public class UserDto : BaseDto<UserDto, User>, IValidatableObject
    {
        [Required]
        [StringLength(100)]
        public string UserName { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(500)]
        public string Password { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        public int Age { get; set; }

        public GenderType Gender { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (UserName.Equals("test", StringComparison.OrdinalIgnoreCase))
                yield return new ValidationResult("نام کاربری نمیتواند Test باشد", new[] { nameof(UserName) });
            if (Password.Equals("123456"))
                yield return new ValidationResult("رمز عبور نمیتواند 123456 باشد", new[] { nameof(Password) });
        }
    }
    public class UserUpdateDto 
    {
        public string FullName { get; set; }
        public int Age { get; set; }
        public GenderType Gender { get; set; }
        public string Email { get; set; }
    }

    public class UserResponseDto
    {
        public string FullName { get; set; }
        public int Age { get; set; }
        public GenderType Gender { get; set; }
        public DateTimeOffset? LastLoginDate { get; set; }
        public int NumberOfBorrow { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }
}