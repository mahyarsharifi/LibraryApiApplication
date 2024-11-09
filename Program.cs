using Common.Exeptions;
using Common;
using Data;
using Data.Contracts;
using Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog.Extensions.Logging;
using NLog.Web;
using System.Net;
using System.Security.Claims;
using System.Text;
using WebFramework.Middlewares;
using Common.Utilities;
using Microsoft.AspNetCore.Mvc.Authorization;
using ElmahCore.Mvc;
using ElmahCore.Sql;
using Services;
using WebFramework.Configuration;
using Entities;
using WebFramework.CustomMapping;

namespace LibraryApplication_Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            SiteSettings siteSettings = new();
            builder.Configuration.GetRequiredSection("SiteSettings").Bind(siteSettings);

            builder.Services.AddSingleton(siteSettings);

            // Add services to the container.

            var connectionString = builder.Configuration.GetConnectionString("LibraryApplication-ApiDb");
            var connectionStringElmah = builder.Configuration.GetConnectionString("Elmah");

            builder.Services.AddDbContext<LibraryApplicationApiDbContext>(x => x.UseSqlServer(connectionString));

            builder.Services.AddCustomIdentity(siteSettings.IdentitySettings);

            builder.Services.InitializeAutoMapper();

            builder.Services.AddMvc(options =>
            {
                options.Filters.Add(new AuthorizeFilter());
            }).SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Latest);


            builder.Services.AddElmah<SqlErrorLog>(options =>
            {
                options.Path = new SiteSettings().ElmahPath;
                options.ConnectionString = connectionStringElmah;
            });

            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IBookRepository, BookRepository>();
            builder.Services.AddScoped<IJwtService, JwtService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            Common.JwtSettings jwtSettings = new();
            builder.Configuration.GetRequiredSection("SiteSettings:JwtSettings").Bind(jwtSettings);

            // ***
            builder.Services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                var secretkey = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
                var encryptionkey = Encoding.UTF8.GetBytes(jwtSettings.EncryptKey);

                var validationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.Zero, // default: 5 min
                    RequireSignedTokens = true,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secretkey),

                    RequireExpirationTime = true,
                    ValidateLifetime = true,

                    ValidateAudience = true, //default : false
                    ValidAudience = siteSettings.JwtSettings.Audience,

                    ValidateIssuer = true, //default : false
                    ValidIssuer = siteSettings.JwtSettings.Issuer,

                    TokenDecryptionKey = new SymmetricSecurityKey(encryptionkey)
                };

                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = validationParameters;
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        //var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                        //logger.LogError("Authentication failed.", context.Exception);

                        if (context.Exception != null)
                            throw new AppExeption(ApiResultStatusCode.UnAuthorized, "Authentication failed.", HttpStatusCode.Unauthorized, context.Exception, null);

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<User>>();
                        var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();

                        var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
                        if (claimsIdentity.Claims?.Any() != true)
                            context.Fail("This token has no claims.");

                        var securityStamp = claimsIdentity.FindFirstValue(new ClaimsIdentityOptions().SecurityStampClaimType);
                        if (!securityStamp.HasValue())
                            context.Fail("This token has no secuirty stamp");

                        //Find user and token from database and perform your custom validation
                        var userId = claimsIdentity.GetUserId<int>();
                        var user = await userRepository.GetByIdAsync(context.HttpContext.RequestAborted, userId);

                        //if (user.SecurityStamp != Guid.Parse(securityStamp))
                        //    context.Fail("Token secuirty stamp is not valid.");

                        var validatedUser = await signInManager.ValidateSecurityStampAsync(context.Principal);
                        if (validatedUser == null)
                            context.Fail("Token secuirty stamp is not valid.");

                        if (!user.IsActive)
                            context.Fail("User is not active.");

                        await userRepository.UpdateLastLoginDateStampAsync(user, context.HttpContext.RequestAborted);
                    },
                    //OnChallenge = context =>
                    //{
                    //    //var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(JwtBearerEvents));
                    //    //logger.LogError("OnChallenge error", context.Error, context.ErrorDescription);

                    //    if (context.AuthenticateFailure != null)
                    //        throw new AppExeption(ApiResultStatusCode.UnAuthorized, "Authenticate failure.", HttpStatusCode.Unauthorized, context.AuthenticateFailure, null);
                    //    throw new AppExeption(ApiResultStatusCode.UnAuthorized, "You are unauthorized to access this resource.", HttpStatusCode.Unauthorized);

                    //    //return Task.CompletedTask;
                    //}
                };
            });

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddNLog();
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseDeveloperExceptionPage();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.Configuration.GetSection(nameof(SiteSettings));

            app.UseCustomExceptionHandler();

            app.UseHttpsRedirection();

            app.UseElmah();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
