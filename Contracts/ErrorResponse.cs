namespace Contracts
{
    [MessageType(MESSAGE_TYPE)]
    public class ErrorResponse
    {
        public const string MESSAGE_TYPE = "Error";
        public string Code { get; init; }
    }
}
