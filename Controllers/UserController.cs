using Common.Exeptions;
using Data.Contracts;
using Entities;
using Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Api;
using Microsoft.AspNetCore.Identity;
using LibraryApplication_Api.Models;
using WebFramwork.Api;
using System.Security.Claims;

namespace ApiApllication.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class UserController : BaseController
    {
        private readonly IUserRepository userRepository;
        private readonly IJwtService jwtService;
        private readonly UserManager<User> userManager;
        private readonly RoleManager<Role> roleManager;
        private readonly SignInManager<User> signInManager;

        public UserController(IUserRepository userRepository, IJwtService jwtService,
            UserManager<User> userManager, RoleManager<Role> roleManager, SignInManager<User> signInManager)
        {
            this.userRepository = userRepository;
            this.jwtService = jwtService;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.signInManager = signInManager;
        }

        [HttpGet]
        //[Authorize(Roles = "Admin")]
        public virtual async Task<ActionResult<List<UserResponseDto>>> Get(CancellationToken cancellationToken)
        {
            var users = await userRepository.GetUsers(cancellationToken);

            if (users == null)
                return NotFound();

            var userDtos = users.Select(user => new UserResponseDto
            {
                FullName = user.FullName,
                Age = user.Age,
                Gender = user.Gender,
                LastLoginDate = user.LastLoginDate,
                NumberOfBorrow = user.NumberOfBorrow,
                UserName = user.UserName,
                Email = user.Email
            }).ToList();

            return Ok(userDtos);
        }

        [HttpPost("[action]")]
        [AllowAnonymous]
        //[Authorize]
        public async Task<ActionResult> Token([FromForm] TokenRequest tokenRequest, CancellationToken cancellationToken)
        {
            //if (String.IsNullOrWhiteSpace(tokenRequest.username) || String.IsNullOrWhiteSpace(tokenRequest.password))
            //    throw new BadRequestExeption("نام کاربری یا رمز عبور نمیتواند خالی باشد");

            var user = await userManager.FindByNameAsync(tokenRequest.username);
            if (user == null)
                throw new BadRequestExeption("کاربری با این نام کاربری و رمز عبور یافت نشد");

            var isPasswordValid = await userManager.CheckPasswordAsync(user, tokenRequest.password);
            if (!isPasswordValid)
                throw new BadRequestExeption("نام کاربری یا رمز عبور اشتباه است");

            var jwt = await jwtService.GenerateAsync(user);
            return new JsonResult(jwt);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ApiResult<UserResponseDto>> Create(UserDto userDto, CancellationToken cancellationToken)
        {
            var user = new User
            {
                Age = userDto.Age,
                FullName = userDto.FullName,
                Gender = userDto.Gender,
                UserName = userDto.UserName,
                Email = userDto.Email
            };

            if (userRepository.TableNoTracking.Any(x => x.UserName == user.UserName))
                throw new BadRequestExeption("کاربری با این نام کاربری وجود دارد");

                var result = await userManager.CreateAsync(user, userDto.Password);

            var result2 = await roleManager.CreateAsync(new Role
            {
                Name = "Admin",
                Description = "Admin Role"
            });

            var result3 = await userManager.AddToRoleAsync(user, "Admin");

            var userResponse = new UserResponseDto
            {
                FullName = user.FullName,
                Age = user.Age,
                Gender = user.Gender,
                LastLoginDate = user.LastLoginDate,
                NumberOfBorrow = user.NumberOfBorrow,
                UserName = user.UserName,
                Email = user.Email
            };

            return userResponse;

            //await userRepository.AddAsync(user, userDto.Password, cancellationToken);
            //return user;
        }

        [HttpPut/*("{id}")*/]
        public async virtual Task<ActionResult> Update(/*int id,*/ UserUpdateDto user, CancellationToken cancellationToken)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var updateUser = await userRepository.GetByIdAsync(cancellationToken, userId);

            if (updateUser == null)
                throw new BadRequestExeption("کاربر مورد نظر با این آیدی یافت نشد");

            updateUser.FullName = user.FullName;
            updateUser.Age = user.Age;
            updateUser.Gender = user.Gender;
            updateUser.Email = user.Email;

            await userRepository.UpdateAsync(updateUser, cancellationToken);


            var userResponse = new UserResponseDto
            {
                FullName = updateUser.FullName,
                Age = updateUser.Age,
                Gender = updateUser.Gender,
                LastLoginDate = updateUser.LastLoginDate,
                NumberOfBorrow = updateUser.NumberOfBorrow,
                UserName = updateUser.UserName,
                Email = updateUser.Email
            };

            return Ok(userResponse);
        }
    }
}
