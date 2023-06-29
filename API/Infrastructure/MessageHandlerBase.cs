namespace API.Infrastructure
{
    public abstract class MessageHandlerBase<TReq, TRes> : IMessageHandler<TReq, TRes>
    {
        public abstract Task<TRes> Handle(TReq req, OperationContext context);
        public async Task<object?> Handle(object request, OperationContext context)
            => await Handle((TReq)request, context);
    }
}
