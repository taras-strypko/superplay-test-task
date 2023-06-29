using API.Infrastructure;
using Contracts;
using Domain;

namespace API.Features
{
    public class LoginHandler : MessageHandlerBase<LoginRequest, LoginResponse>
    {
        private readonly PlayerRepository playerRepository;
        private readonly PlayersPool playersPool;

        public LoginHandler(PlayerRepository playerRepository, PlayersPool communicationManager)
        {
            this.playerRepository = playerRepository;
            this.playersPool = communicationManager;
        }

        public override Task<LoginResponse> Handle(LoginRequest req, OperationContext context)
        {
            var player = playerRepository.Get(req.DeviceId);

            player ??= playerRepository.Create(req.DeviceId);

            var res = player.Login();

            if (res != LoginResult.OK)
            {
                throw OperationException.Create(res);
            }

            // should be event, but not this time :)
            playersPool.PlayerLoggedIn(player.Id, context.Channel!);

            return Task.FromResult(new LoginResponse { PlayerId = player.Id });
        }
    }
}
