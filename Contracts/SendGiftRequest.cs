using Domain;

namespace Contracts
{
    [MessageType(MESSAGE_TYPE)]
    public class SendGiftRequest
    {
        public const string MESSAGE_TYPE = "SendGiftRequest";
        public long FriendPlayerId { get; init; }

        public ResourceType Type { get; init; }

        public int Amount { get; init; }
    }
}
