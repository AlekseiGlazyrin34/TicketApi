using System;
using System.Collections.Generic;

namespace TicketApi;

public partial class Request
{
    public int RequestId { get; set; }

    public int UserId { get; set; }

    public string? Description { get; set; }

    public int PriorityId { get; set; } 

    public string ProblemName { get; set; } = null!;

    public string Room { get; set; } = null!;

    public int StatusId { get; set; }

    public DateTime Reqtime { get; set; }

    public int? ResponseId { get; set; }

    public virtual Priority Priority { get; set; } = null!;

    public virtual Response? Response { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
