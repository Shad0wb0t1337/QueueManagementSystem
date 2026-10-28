namespace QueueSystem.API.Models;

public enum TicketStatus
{
    Waiting = 0,
    Called = 1,
    Serving = 2,
    Done = 3,
    NoShow = 4,
    RecallPending = 5
}
