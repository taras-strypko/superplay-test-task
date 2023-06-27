namespace Contracts
{
    public class Message
    {
        public string Type { get; init; }
        public string Payload { get; init; }

        public long? PlayerId { get; init; }
    }
}
