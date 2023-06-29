using API.Infrastructure;
using Contracts;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace API.Features
{
    [Authorize]
    public class UpdateResourceHandler : MessageHandlerBase<UpdateResourceRequest, UpdateResourceResponse>
    {
        private readonly PlayerRepository playerRepository;

        public UpdateResourceHandler(PlayerRepository playerRepository)
        {
            this.playerRepository = playerRepository;
        }

        public override Task<UpdateResourceResponse> Handle(UpdateResourceRequest req, OperationContext context)
        {
            var player = playerRepository.Get(context.PlayerId!.Value);

            if (player == null)
            {
                throw OperationException.Create(Errors.PLAYER_NOT_FOUND);
            }

            var res = player.UpdateResource(req.ResourceType, req.ResourceValue);

            if (res != Domain.UpdateResourceResult.OK)
            {
                throw OperationException.Create(res);
            }

            return Task.FromResult(new UpdateResourceResponse
            {
                Balance = player.Resources.Select(c => new ResourceDto
                {
                    Type = c.Key,
                    Amount = c.Value
                }).ToArray()
            });
        }
    }
}
