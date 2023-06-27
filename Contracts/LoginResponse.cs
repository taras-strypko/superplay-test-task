namespace Contracts
{
    [MessageType(MESSAGE_TYPE)]
    public class LoginResponse
    {
        public const string MESSAGE_TYPE = "LoginResponse";
        public long PlayerId { get; init; }
    }
}
