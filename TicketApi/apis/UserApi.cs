

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
                    Password =pers.Password,
                    Role = pers.Role
                };
               
                pers.RefreshToken = RefrToken;
                pers.RefreshTokenExpireTime = DateTime.UtcNow.AddSeconds(15);
                db.SaveChanges();
                return Results.Json(response);
                //Console.WriteLine(loginData.Login+" "+loginData.Password);
            });

            app.MapPost("/refresh-token", async (User request) =>
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

            app.Map("/data", [Authorize(Roles ="User")] (HttpContext context) => 
            
                $"Hello World!"

            );
        }

        public static string GenerateAccessToken(User pers)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, pers.Login),
                    new Claim(ClaimTypes.Role,pers.Role)
                };
            var claimsIdentity = new ClaimsIdentity(claims, "Token");
            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromSeconds(10)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
