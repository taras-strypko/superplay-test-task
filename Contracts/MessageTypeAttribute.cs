namespace Contracts
{
    public class MessageTypeAttribute : Attribute
    {
        public string Name { get; }

        public MessageTypeAttribute(string name)
        {
            Name = name;
        }
    }
}
