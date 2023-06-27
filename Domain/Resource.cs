namespace Domain
{
    public class Resource
    {
        public ResourceType Type { get; }
        public int Amount { get; }

        public Resource(ResourceType type, int amount)
        {
            Type = type;
            Amount = amount >= 0 ? amount : throw new ArgumentOutOfRangeException($"{nameof(amount)} cannot be negative");
        }
    }

    public enum ResourceType
    {
        Coins = 1,
        Rolls = 2
    }
}
