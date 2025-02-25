﻿

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mail;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;


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
                
            });


            app.MapPost("/refresh-token", async (HttpContext context) =>
            {
                TicketsystemContext db = new TicketsystemContext();
                using var reader = new StreamReader(context.Request.Body);
                string refrtok = await reader.ReadToEndAsync();
                
                var pers = db.Users.FirstOrDefault(u => u.Refreshtoken == refrtok);

                if (pers == null || pers.Refreshtokenexpiretime < DateTime.UtcNow.ToLocalTime())
                {
                    return Results.Content("LoginAgain");
                }
                var newAccessToken = GenerateAccessToken(pers);

                

                return Results.Content(newAccessToken); 
            });

            app.MapPost("/send-request", [Authorize(Roles ="User,Admin")] (HttpContext context,RequestDto req) =>
            {
                TicketsystemContext db = new TicketsystemContext();
                var userId = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                int PrId = db.Priorities.FirstOrDefault(p => p.PriorityName == req.Priority).PriorityId;
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

            app.MapGet("/load-data", [Authorize(Roles = "User")] (HttpContext context) =>
            {
                TicketsystemContext db = new TicketsystemContext();
                var userId = Convert.ToInt32(context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
                var reqs = db.Requests
                    .Include(r=> r.Status)
                    .Where(r=>r.UserId == userId)
                    .Select(r => new {r.RequestId,r.ProblemName,r.Status.StatusName,r.Reqtime})
                    .ToList();
                return Results.Json(reqs);
            });

            app.MapGet("/loadadd-data", [Authorize(Roles = "User,Admin")] (HttpContext context, int reqid) =>
            {
                TicketsystemContext db = new TicketsystemContext();
               
                var req = db.Requests
                    .Include(r => r.Status)
                    .Include(r => r.Priority)
                    .Where(r => r.RequestId == reqid)
                    .Select(r => new { r.RequestId, r.ProblemName, r.Status.StatusName, r.Priority.PriorityName, r.Description, r.Reqtime, r.Room, r.Response.ResponseContent, respusername= r.Response.User.Username,r.User.Username });
                return Results.Json(req);
            });

            app.MapGet("/load-alldata", [Authorize(Roles = "Admin")] (HttpContext context) =>
            {
                TicketsystemContext db = new TicketsystemContext();
                
                var reqs = db.Requests
                    .Include(r => r.Status)
                    .Select(r => new { r.RequestId, r.ProblemName, r.Status.StatusName, r.Reqtime })
                    .ToList();
                return Results.Json(reqs);
            });

            app.MapPost("/save-changes", [Authorize(Roles = "Admin")] (HttpContext context,Changes ch) =>
            {
                TicketsystemContext db = new TicketsystemContext();
                var userId = Convert.ToInt32(context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
                Console.WriteLine(ch.ReqId+ ch.StatusName+ ch.ResponseContent);
                var requestToUpdate = db.Requests.FirstOrDefault(r => r.RequestId == ch.ReqId);

                if (requestToUpdate != null)
                {
                    var resp = new Response
                    {
                        ResponseContent = ch.ResponseContent,
                        UserId = userId
                    };
                    
                    requestToUpdate.StatusId = db.Statuses.FirstOrDefault(s=> s.StatusName==ch.StatusName).StatusId;
                    requestToUpdate.Response = resp;
                    db.SaveChanges();
                }
            });
        }



        public static string GenerateAccessToken(User pers)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, Convert.ToString(pers.UserId)),
                    new Claim(ClaimTypes.Role,pers.Role.RoleName)
                };
            var claimsIdentity = new ClaimsIdentity(claims, "Token");
            // создаем JWT-токен
            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(15)),
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
    public class Changes
    {
        public int ReqId { get; set; }
        public string StatusName { get; set; } = null!;
        public string ResponseContent { get; set; } = null!;
    }

}
