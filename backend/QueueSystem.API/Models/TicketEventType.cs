namespace QueueSystem.API.Models;

public enum TicketEventType
{
    Created = 0,
    Called = 1,
    ServingStarted = 2,
    Completed = 3,
    NoShow = 4,
    RecallPending = 5,
    Recalled = 6,
    Redirected = 7
}
