using Domain;

namespace Contracts
{
    [MessageType(MESSAGE_TYPE)]
    public class UpdateResourceRequest
    {
        public const string MESSAGE_TYPE = "UpdateResourceRequest";
        public ResourceType ResourceType { get; init; }

        public int ResourceValue { get; init; }
    }
}
