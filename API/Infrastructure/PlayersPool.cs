namespace API.Infrastructure
{
    public class PlayersPool
    {
        private Dictionary<long, PlayerChannel> playersOnione = new();

        public void PlayerLoggedIn(long playerId, PlayerChannel channel)
        {
            channel.OnClose += () => PlayerLoggedOut(playerId);
            playersOnione[playerId] = channel;
        }

        public void PlayerLoggedOut(long playerId)
        {
            playersOnione.Remove(playerId);
        }

        public async Task SendMessageTo<T>(long playerId, T payload)
        {
            if (!playersOnione.ContainsKey(playerId))
            {
                return;
            }

            var channel = playersOnione[playerId];

            await channel.SendMessage(payload);
        }
    }
}
