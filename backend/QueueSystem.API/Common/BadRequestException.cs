namespace QueueSystem.API.Common;

public class BadRequestException : ApiException
{
    public BadRequestException(string message)
        : base(message, StatusCodes.Status400BadRequest)
    {
    }
}
