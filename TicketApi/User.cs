using System;
using System.Collections.Generic;

namespace TicketApi;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string JobTitle { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
