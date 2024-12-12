using System;
using System.Collections.Generic;

namespace TicketApi;

public partial class Priority
{
    public int PriorityId { get; set; }

    public string PriorityTitle { get; set; } = null!;

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
