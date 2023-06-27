namespace API.Infrastructure
{
    public interface IMessageHandler<TReq, TRes>
    {
        Task<TRes> Handle(TReq req, OperationContext context);
    }
}
