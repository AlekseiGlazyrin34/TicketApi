

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mail;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;
using System.Data;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;

namespace TicketApi
{
    
    public static class UserApi
    {
        public static void MapRoutes(WebApplication app) {

            app.MapPost("/register", [Authorize(Roles = "Admin")] (HttpContext context, RegisterDto registerDto) =>
            {
                
                TicketsystemContext db = new TicketsystemContext();
                // Проверка существования пользователя
                Console.WriteLine(registerDto.Username + " " + registerDto.Login + " " + " " + registerDto.JobId + " " + registerDto.RoleId);
                User? pers = db.Users.FirstOrDefault(u => u.Login == registerDto.Login);
                if (pers != null)
                {
                    return Results.BadRequest("Пользователь с таким логином уже существует");
                }

                // Хеширование пароля
                var hashedPassword = HashPassword(registerDto.Password);
                
                // Создание нового пользователя
                var user = new User
                {
                    Username = registerDto.Username,
                    Login = registerDto.Login,
                    Password = hashedPassword,
                    JobId = registerDto.JobId,
                    RoleId = registerDto.RoleId
                };

                db.Users.Add(user);
                db.SaveChanges();

                return Results.Ok(new { message = "Регистрация успешна" });
            });

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
                    .FirstOrDefault(us => us.Login == loginData.Login);

                var inputPasswordHash = HashPassword(loginData.Password);
                if (pers.Password != inputPasswordHash)
                    return Results.Unauthorized();
                

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
                pers.Refreshtokenexpiretime = DateTime.UtcNow.AddDays(7).ToLocalTime(); ;
                db.SaveChanges();
                return Results.Json(response);
                
            });


            app.MapPost("/refresh-token", async (HttpContext context) =>
            {
                TicketsystemContext db = new TicketsystemContext();
                using var reader = new StreamReader(context.Request.Body);
                string refrtok = await reader.ReadToEndAsync();
                
                var pers = db.Users
                .Include(u=>u.Role)
                .FirstOrDefault(u => u.Refreshtoken == refrtok);

                if (pers == null || pers.Refreshtokenexpiretime < DateTime.UtcNow.ToLocalTime())
                {
                    return Results.Unauthorized();
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
                    .OrderByDescending(r =>r.Reqtime )
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

            app.MapGet("/load-alldata", [Authorize(Roles = "Admin")] (HttpContext context, [FromQuery] int? userId) =>
            {
                using var db = new TicketsystemContext();

                var query = db.Requests
                    .Include(r => r.Status)
                    .AsQueryable();

                if (userId.HasValue)
                    query = query.Where(r => r.UserId == userId.Value); // предполагаем, что у Request есть поле UserId

                var reqs = query
                    .Select(r => new { r.RequestId, r.ProblemName, r.Status.StatusName, r.Reqtime })
                    .OrderByDescending(r=>r.Reqtime)
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
                    if (ch.CreateChat)
                    {
                        var chat = db.Chats.FirstOrDefault(c => c.RequestId == requestToUpdate.RequestId);
                        if (chat == null)
                        {
                            chat = new Chat
                            {
                                UserId = requestToUpdate.UserId,
                                AdminId = userId,
                                LastMessage = requestToUpdate.ProblemName,
                                LastUpdated = DateTime.Now,
                                RequestId = ch.ReqId
                            };
                            db.Chats.Add(chat);
                        }
                        else return Results.Content("ChatAlreadyExist");
                        
                    }
                    db.SaveChanges();
                    return Results.Ok();
                }
                return Results.NotFound();
            });

            app.MapGet("/get-adminchats", [Authorize(Roles = "Admin")] (HttpContext context) =>
            {
                var db = new TicketsystemContext();
                var userId = int.Parse(context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
                var chats = db.Chats
                    .Include(c=>c.Request)
                    .Where(c => c.AdminId == userId || c.UserId == userId)
                    .Select(c => new { c.ChatId, UserName = c.User.Username, c.LastMessage, c.LastUpdated,c.Request.ProblemName})
                    .OrderByDescending(c => c.LastUpdated)
                    .ToList();
                Console.WriteLine(chats.Count);
                return Results.Json(chats);
            });

            app.MapGet("/get-chats", [Authorize(Roles = "User")] (HttpContext context) =>
            {
                var db = new TicketsystemContext();
                var userId = int.Parse(context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
                var chats = db.Chats
                    .Include(c => c.Request)
                    .Where(c => c.UserId == userId)
                    .Select(c => new { c.ChatId, UserName = c.Admin.Username,c.LastMessage,c.LastUpdated, c.Request.ProblemName })
                    .OrderByDescending(c => c.LastUpdated)
                    .ToList();
                Console.WriteLine(chats.Count);
                return Results.Json(chats);
            });

            app.MapGet("/get-messages", [Authorize(Roles = "User,Admin")] (int chatId) =>
            {
                var db = new TicketsystemContext();
                var messages = db.Messages
                    .Where(m => m.ChatId == chatId)
                    .Select(m => new { m.Content, SenderName = m.Sender.Username, m.SentTime })
                    .ToList();
                return Results.Json(messages);
            });

            app.MapPost("/send-message", [Authorize(Roles = "User,Admin")] (HttpContext context, MessageDto dto) =>
            {
                var db = new TicketsystemContext();
                var senderId = int.Parse(context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

                var message = new Message
                {
                    ChatId = dto.ChatId,
                    SenderId = senderId,
                    Content = dto.Content,
                    SentTime = DateTime.Now
                };
                db.Messages.Add(message);
                var updatedChat =  db.Chats.FirstOrDefault(c=>c.ChatId == dto.ChatId);
                updatedChat.LastMessage = message.Content;
                updatedChat.LastUpdated = DateTime.Now;
                db.SaveChanges();
                return Results.Ok();
            });

            app.MapGet("/get-admins", [Authorize(Roles = "Admin")] (HttpContext context) =>
            {
                var db = new TicketsystemContext();
                var users = db.Users
                    .Where(u => u.RoleId == 1)
                    .Select(u => new { u.UserId, u.Username })
                    .ToList();
                return Results.Json(users);
            });
            app.MapGet("/get-users", [Authorize(Roles = "Admin")] () =>
            {
                using var db = new TicketsystemContext();
                var users = db.Users.Select(u => new { u.UserId, u.Username }).ToList();
                return Results.Json(users);
            });

            app.MapPost("/create-chat", [Authorize(Roles = "Admin")] (HttpContext context, int userId) =>
            {
                var db = new TicketsystemContext();
                var adminId = int.Parse(context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
                var chat = new Chat
                {
                    UserId = userId,
                    AdminId = adminId,
                    LastMessage = "",
                    LastUpdated = DateTime.Now
                };
                db.Chats.Add(chat);
                db.SaveChanges();
            });

        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
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
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(60)),
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
        public bool CreateChat { get; set; } 
    }
    public class MessageDto
    {
        public int ChatId { get; set; }
        public string Content { get; set; } = null!;
    }
 
        public class RegisterDto
        {
            public string Username { get; set; }
            public string Login { get; set; }
            public string Password { get; set; }
            public int JobId { get; set; }
            public int RoleId { get; set; }
        }   
    }
    