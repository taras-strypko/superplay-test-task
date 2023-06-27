using API.Infrastructure;
using Contracts;
using Microsoft.AspNetCore.Authorization;

namespace API.Features
{
    [Authorize]
    public class SendGiftHandler : IMessageHandler<SendGiftRequest, SendGiftResponse>
    {
        private readonly PlayerRepository playerRepository;
        private readonly PlayersPool communicationManager;

        public SendGiftHandler(PlayerRepository playerRepository, PlayersPool communicationManager)
        {
            this.playerRepository = playerRepository;
            this.communicationManager = communicationManager;
        }

        public async Task<SendGiftResponse> Handle(SendGiftRequest req, OperationContext context)
        {
            var player = playerRepository.Get(context.PlayerId!.Value) ?? throw OperationException.Create(Errors.PLAYER_NOT_FOUND);
            var friend = playerRepository.Get(req.FriendPlayerId) ?? throw OperationException.Create(Errors.FRIEND_NOT_FOUND);

            var result = player.SendGift(friend, req.Type, req.Amount);

            if (result != Domain.SendGiftResult.OK)
            {
                throw OperationException.Create(result);
            }

            if (friend.IsOnline)
            {
                await communicationManager.SendMessageTo(friend.Id, new GiftReceived
                {
                    FromFriendId = player.Id,
                    Resource = new ResourceDto
                    {
                        Type = req.Type,
                        Amount = req.Amount,
                    }
                });
            }

            return new SendGiftResponse();
        }
    }
}
