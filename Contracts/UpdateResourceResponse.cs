using Domain;

namespace Contracts
{
    [MessageType(MESSAGE_TYPE)]
    public class UpdateResourceResponse
    {
        public const string MESSAGE_TYPE = "UpdateResourceResponse";
        public ResourceDto[] Balance { get; init; } = Array.Empty<ResourceDto>();
    }

    public class ResourceDto
    {
        public ResourceType Type { get; init; }

        public int Amount { get; init; }
    }

    public enum UpdateResourceStatus
    {
        OK = 0,
        INVALID_AMOUNT = 1,
        INVALID_TYPE = 2,
        PLAYER_NOT_FOUND = 3
    }
}
