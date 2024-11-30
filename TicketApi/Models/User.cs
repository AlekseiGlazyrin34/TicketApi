using System;
using System.Collections.Generic;

namespace TicketApi.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string JobTitle { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? Refreshtoken { get; set; }

    public DateTime? Refreshtokenexpiretime { get; set; }

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
