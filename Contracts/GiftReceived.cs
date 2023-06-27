namespace Contracts
{
    [MessageType(MESSAGE_TYPE)]
    public class GiftReceived
    {
        public const string MESSAGE_TYPE = "GiftReceived";
        public long FromFriendId { get; init; }

        public ResourceDto Resource { get; init; }
    }
}
