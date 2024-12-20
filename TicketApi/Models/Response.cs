

namespace TicketApi;

public partial class Response
{
    public int ResponseId { get; set; }

    public string ResponseContent { get; set; } = null!;

    public int UserId { get; set; }

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual User User { get; set; } = null!;
}
