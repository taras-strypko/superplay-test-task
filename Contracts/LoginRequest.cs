namespace Contracts
{
    [MessageType(MESSAGE_TYPE)]
    public class LoginRequest
    {
        public const string MESSAGE_TYPE = "LoginRequest";
        public Guid DeviceId { get; init; }
    }
}