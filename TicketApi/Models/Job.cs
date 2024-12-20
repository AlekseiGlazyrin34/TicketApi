

namespace TicketApi;

public partial class Job
{
    public int JobId { get; set; }

    public string JobTitle { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
