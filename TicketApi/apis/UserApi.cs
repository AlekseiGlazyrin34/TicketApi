

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TicketApi.Models;

namespace TicketApi
{
    
    public static class UserApi
    {
        public static void MapRoutes(WebApplication app) {

            app.MapPost("/login", (User loginData) =>
            {

                TicketsystemContext db = new TicketsystemContext();
                var users = db.Users.ToList();
                User? pers = users.FirstOrDefault(us => us.Login == loginData.Login && us.Password == loginData.Password);
                if (pers == null) return Results.Unauthorized();

                var encodedJwt = GenerateAccessToken(pers);
                var RefrToken = Guid.NewGuid().ToString();

                // формируем ответ
                var response = new
                {
                    Token = encodedJwt,
                    RefreshToken = RefrToken,
                    Username = pers.Username,
                    JobTitle = pers.JobTitle,
                    Login = pers.Login,
                    Password = pers.Password,
                    Role = pers.Role,
                    UserId = Convert.ToString(pers.UserId)
                };
               
                pers.RefreshToken = RefrToken;
                pers.RefreshTokenExpireTime = DateTime.UtcNow.AddHours(24);
                db.SaveChanges();
                return Results.Json(response);
                //Console.WriteLine(loginData.Login+" "+loginData.Password);
            });


            app.MapPost("/refresh-token", (User request) =>
            {
                TicketsystemContext db = new TicketsystemContext();
                // Проверяем, существует ли пользователь с этим Refresh Token
                var pers = db.Users.FirstOrDefault(u => u.RefreshToken == request.RefreshToken);

                if (pers == null || pers.RefreshTokenExpireTime < DateTime.UtcNow)
                {
                    return Results.Content("LoginAgain");
                }

                // Генерируем новый Access Token
                var newAccessToken = GenerateAccessToken(pers);

                // Можно также обновить Refresh Token (по желанию)

                return Results.Content(newAccessToken);
                    
                
            });

            app.MapPost("/send-request", [Authorize(Roles ="User")] (HttpContext context,Request req) =>
            {
                var userId = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine(userId);
            }
            );
        }


        public static string GenerateAccessToken(User pers)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, Convert.ToString(pers.UserId)),
                    new Claim(ClaimTypes.Role,pers.Role),
                    
                };
            var claimsIdentity = new ClaimsIdentity(claims, "Token");
            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(45)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
