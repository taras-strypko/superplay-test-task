using Contracts;

namespace API.Infrastructure
{
    public interface IMessageHandler
    {
        Task<object?> Handle(object Request, OperationContext context);
    }

    public interface IMessageHandler<TReq, TRes> : IMessageHandler
    {
        Task<TRes> Handle(TReq Req, OperationContext context);
    }
}
