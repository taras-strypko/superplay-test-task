namespace Contracts
{
    [MessageType(MESSAGE_TYPE)]
    public class ErrorResponse
    {
        public const string MESSAGE_TYPE = "Error";
        public string Code { get; }

        public ErrorResponse(string code)
        {
            this.Code = code;
        }
    }
}
