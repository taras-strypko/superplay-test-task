namespace API.Infrastructure
{
    public class OperationException : Exception
    {
        public string Code { get; init; }

        private OperationException(string code)
        {
            Code = code;
        }

        public static OperationException Create<T>(T code)
            where T : Enum
        {
            return new OperationException(Enum.GetName(code.GetType(), code)!);
        }
    }
}
