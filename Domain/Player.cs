namespace Domain
{
    public class Player
    {
        public long Id { get; private set; }
        public Guid DeviceId { get; private set; }
        public bool IsOnline { get; private set; }
        public Dictionary<ResourceType, int> Resources { get; private set; } = new Dictionary<ResourceType, int>();

        public Player(long id, Guid deviceId)
        {
            Id = id;
            DeviceId = deviceId;
        }

        public LoginResult Login()
        {
            if (IsOnline)
            {
                return LoginResult.PLAYER_IS_ALREADY_ONLINE;
            }

            IsOnline = true;

            return LoginResult.OK;
        }

        public UpdateResourceResult UpdateResource(ResourceType type, int amount)
        {
            if (!Enum.IsDefined(typeof(ResourceType), type))
            {
                return UpdateResourceResult.INVALID_RESOURCE_TYPE;
            }

            if (amount == 0)
            {
                return UpdateResourceResult.INVALID_AMOUNT;
            }

            if (!Resources.ContainsKey(type))
            {
                var resultingAmount = amount > 0 ? amount : 0;
                Resources.Add(type, resultingAmount);
            }
            else
            {
                var availableAmount = Resources[type];
                var resultingAmount = availableAmount + amount;

                // cant decrease less to than 0
                Resources[type] = resultingAmount < 0 ? 0 : resultingAmount;
            }

            return UpdateResourceResult.OK;
        }

        public SendGiftResult SendGift(Player friend, ResourceType type, int amount)
        {
            if (amount < 0)
            {
                return SendGiftResult.CANNOT_STEAL_FROM_FRIEND; // how dear you ?)
            }

            if (Id == friend.Id)
            {
                return SendGiftResult.CANNOT_SEND_GIFT_TO_YOURSELF; // selfish
            }

            var hasThisTypeOfResource = Resources.TryGetValue(type, out var availableAmount);

            if (!hasThisTypeOfResource || availableAmount < amount)
            {
                return SendGiftResult.INSUFFICIENT_RESOURCES;
            }

            Resources[type] -= amount;

            friend.UpdateResource(type, amount);

            return SendGiftResult.OK;
        }

        public void Logout()
        {
            IsOnline = false;
        }
    }

    public enum LoginResult
    {
        OK = 0,
        PLAYER_NOT_FOUND = 1,
        PLAYER_IS_ALREADY_ONLINE = 2
    }

    public enum UpdateResourceResult
    {
        OK = 0,
        INVALID_AMOUNT = 1,
        INVALID_RESOURCE_TYPE = 2
    }

    public enum SendGiftResult
    {
        OK = 0,
        INSUFFICIENT_RESOURCES = 1,
        CANNOT_STEAL_FROM_FRIEND = 2,
        CANNOT_SEND_GIFT_TO_YOURSELF = 3
    }
}
