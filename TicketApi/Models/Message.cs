using System;
using System.Collections.Generic;

namespace TicketApi;

public partial class Message
{
    public int MessageId { get; set; }

    public int ChatId { get; set; }

    public int SenderId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime SentTime { get; set; }

    public virtual Chat Chat { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
