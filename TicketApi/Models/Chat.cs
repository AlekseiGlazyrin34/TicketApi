using System;
using System.Collections.Generic;

namespace TicketApi;

public partial class Chat
{
    public int ChatId { get; set; }

    public int UserId { get; set; }

    public int? AdminId { get; set; }

    public string LastMessage { get; set; } = null!;

    public DateTime LastUpdated { get; set; }

    public virtual User? Admin { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual User User { get; set; } = null!;
}
