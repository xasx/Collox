namespace Collox.Models;

public class ConversationContribution
{
    public DateTime Timestamp { get; set; }

    public string Message { get; set; }

    public string Participant { get; set; }

    public Guid Id { get; set; }
}
