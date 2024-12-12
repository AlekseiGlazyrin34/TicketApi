

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace TicketApi
{
    
    public static class UserApi
    {
        public static void MapRoutes(WebApplication app) {

            app.MapPost("/login", (LoginDto loginData) =>
            {
                if (string.IsNullOrWhiteSpace(loginData.Login) || string.IsNullOrWhiteSpace(loginData.Password))
                {
                    return Results.BadRequest("Логин и пароль обязательны.");
                }

                TicketsystemContext db = new TicketsystemContext();
                
                User? pers = db.Users
                    .Include(u => u.Role)
                    .Include(u => u.Job)
                    .FirstOrDefault(us => us.Login == loginData.Login && us.Password == loginData.Password);
                if (pers == null) return Results.Unauthorized();

                var encodedJwt = GenerateAccessToken(pers);
                var RefrToken = Guid.NewGuid().ToString();

                // формируем ответ
                var response = new
                {
                    Token = encodedJwt,
                    RefreshToken = RefrToken,
                    Username = pers.Username,
                    JobTitle = pers.Job.JobTitle,
                    Login = pers.Login,
                    Password = pers.Password,
                    Role = pers.Role.RoleName,
                    UserId = Convert.ToString(pers.UserId)
                };
               
                pers.Refreshtoken = RefrToken;
                pers.Refreshtokenexpiretime = DateTime.UtcNow.AddHours(24).ToLocalTime(); ;
                db.SaveChanges();
                return Results.Json(response);
                //Console.WriteLine(loginData.Login+" "+loginData.Password);
            });


            app.MapPost("/refresh-token", (User request) =>
            {
                TicketsystemContext db = new TicketsystemContext();
                // Проверяем, существует ли пользователь с этим Refresh Token
                var pers = db.Users.FirstOrDefault(u => u.Refreshtoken == request.Refreshtoken);

                if (pers == null || pers.Refreshtokenexpiretime < DateTime.UtcNow.ToLocalTime())
                {
                    return Results.Content("LoginAgain");
                }

                // Генерируем новый Access Token
                var newAccessToken = GenerateAccessToken(pers);

                // Можно также обновить Refresh Token (по желанию)

                return Results.Content(newAccessToken); 
            });

            app.MapPost("/send-request", [Authorize(Roles ="User")] (HttpContext context,RequestDto req) =>
            {
                TicketsystemContext db = new TicketsystemContext();
                var userId = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                int PrId = db.Priorities.FirstOrDefault(p => p.PriorityTitle == req.Priority).PriorityId;
                var newReq = new Request
                {
                    UserId = Convert.ToInt32(userId),
                    ProblemName = req.ProblemName,
                    Room = req.Room,
                    PriorityId = PrId,
                    Description = req.Description,
                    StatusId = 1,
                    Reqtime= DateTime.UtcNow.ToLocalTime()
                };
                db.Requests.Add(newReq);
                db.SaveChanges();
                return Results.Content("Запись добавлена");
            }
            );

            
        }



        public static string GenerateAccessToken(User pers)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, Convert.ToString(pers.UserId)),
                    new Claim(ClaimTypes.Role,pers.Role.RoleName),
                    
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
    public class LoginDto
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RequestDto
    {
        public string ProblemName { get; set; } = null!;
        public string Room {  get; set; }= null!;
        public string Priority { get; set; } = null!;
        public string Description { get; set; } =null!;
    }
    
}
