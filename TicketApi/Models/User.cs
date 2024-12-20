
namespace TicketApi;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int JobId { get; set; }

    public int RoleId { get; set; }

    public string? Refreshtoken { get; set; }

    public DateTime? Refreshtokenexpiretime { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<Response> Responses { get; set; } = new List<Response>();

    public virtual Role Role { get; set; } = null!;
}
