

namespace TicketApi
{
    
    public static class UserApi
    {
        public static void MapRoutes(WebApplication app) {

            app.MapPost("apis/users", (User loginData) =>
            {
            TicketsystemContext db = new TicketsystemContext();
            var users = db.Users.ToList();
            User? pesr = users.FirstOrDefault(us => us.Login == loginData.Login && us.Password == loginData.Password);
            if (pesr != null) Results.Content("Провал");
            else Results.Content("Успешно");
            });
        }
    }
}
