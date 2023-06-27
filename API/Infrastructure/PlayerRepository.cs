using Domain;

namespace API.Infrastructure
{
    public class PlayerRepository
    {
        private List<Player> players = new List<Player>();

        public Player? Get(Guid deviceId)
        {
            return players.FirstOrDefault(p => p.DeviceId == deviceId);
        }

        public Player? Get(long playerId)
        {
            return players.FirstOrDefault(p => p.Id == playerId);
        }

        public Player Create(Guid deviceId)
        {
            var id = players.Any() ? players.Max(p => p.Id) + 1 : 1;
            var player = new Player(id, deviceId);

            players.Add(player);

            return player;
        }
    }
}
