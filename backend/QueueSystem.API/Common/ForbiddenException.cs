namespace QueueSystem.API.Common;

public class ForbiddenException : ApiException
{
    public ForbiddenException(string message)
        : base(message, StatusCodes.Status403Forbidden)
    {
    }
}
