namespace API.Infrastructure
{
    public class OperationContext
    {
        public long? PlayerId { get; init; }

        public PlayerChannel? Channel { get; init; }
    }
}
