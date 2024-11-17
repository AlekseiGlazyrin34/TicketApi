using System;
using System.IO;
using TicketApi;


var builder = WebApplication.CreateBuilder();
var app = builder.Build();

/* using (var db = new TicketsystemContext())
{
    User nuser = new User {Username = "Иван", Login="iva", Password="lalala",JobTitle="Учитель", Role="use"};
    db.Users.Add(nuser);
    db.SaveChanges();
}
*/

using ( var db= new TicketsystemContext())
{
    var users = db.Users.ToList();
    foreach (var user in users)
    {
        Console.WriteLine(user.UserId + " " + user.Username + " " + user.Login + " " + user.Password + " " + user.JobTitle + " "+user.Role);
    }

    foreach (var user in users)
    {
        if (user.Login == "iva")
        {
            db.Users.Remove(user);
        }
        db.SaveChanges();
    }

    foreach (var user in users)
    {
        Console.WriteLine(user.UserId + " " + user.Username + " " + user.Login + " " + user.Password + " " + user.JobTitle + " " + user.Role);
    }
}
app.Run();
