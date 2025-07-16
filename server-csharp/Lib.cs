using SpacetimeDB;

public static partial class Module
{
    /////////////////////////////////////////////////////////////
    /// Tables

    [Table(Name = "train", Public = true)]
    public partial struct Train
    {
        [PrimaryKey]
        public string id;
        public string from_station_id;
        public string to_station_id;
        public uint money;
    }

    [Table(Name = "station", Public = true)]
    public partial struct Station
    {
        [PrimaryKey]
        public string id;
        public string name;
        public List<Item> items;
    }

    // Simplified item structure: only id and quantity
    [Table(Name = "item", Public = true)]
    public partial struct Item
    {
        [PrimaryKey, AutoInc]
        public uint id;
        public uint quantity;
    }

    // Player with equipped weapon, armor, and consumables
    [Table(Name = "player", Public = true)]
    [Table(Name = "logged_out_player")]
    public partial struct Player
    {
        [PrimaryKey]
        public Identity identity;

        [Unique, AutoInc]
        public uint player_id;
        public string name;
        public uint money;

        public Item weapon;
        public Item armor;
        public List<Item> consumables;
    }

    /////////////////////////////////////////////////////////////
    /// REDUCERS - INIT / CONNECT / LOGIN

    [Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        Log.Info($"[Init] Initializing...");
    }

    [Reducer(ReducerKind.ClientConnected)]
    public static void Connect(ReducerContext ctx)
    {
        Log.Info($"[Connect] Client connected: {ctx.Sender}");

        var player = ctx.Db.logged_out_player.identity.Find(ctx.Sender);
        if (player.HasValue)
        {
            Log.Info($"[Connect] Found logged out player, moving to active players.");
            ctx.Db.player.Insert(player.Value);
            ctx.Db.logged_out_player.identity.Delete(player.Value.identity);
        }
        else
        {
            Log.Info($"[Connect] No logged out player found, creating new player.");
            ctx.Db.player.Insert(new Player
            {
                identity = ctx.Sender,
                name = "",
                money = 0,
                weapon = new Item { id = 0, quantity = 0 },
                armor = new Item { id = 0, quantity = 0 },
                consumables = new List<Item>()
            });
        }
    }

    [Reducer(ReducerKind.ClientDisconnected)]
    public static void Disconnect(ReducerContext ctx)
    {
        Log.Info($"[Disconnect] Client disconnected: {ctx.Sender}");

        var player = ctx.Db.player.identity.Find(ctx.Sender) ?? throw new Exception("[Disconnect] Player not found");
        ctx.Db.logged_out_player.Insert(player);
        ctx.Db.player.identity.Delete(player.identity);
        Log.Info($"[Disconnect] Player moved to logged_out_player.");
    }

    /////////////////////////////////////////////////////////////
    /// ITEM MANAGEMENT

    [Reducer]
    public static void AddItem(ReducerContext ctx, uint _id)
    {
        Log.Info($"[AddItem] Adding item with ID {_id}");

        var entity = ctx.Db.item.id.Find(_id);
        if (entity.HasValue)
        {
            var updated = entity.Value;
            updated.quantity++;
            ctx.Db.item.id.Update(updated);
            Log.Info($"[AddItem] Increased quantity of item {_id}. Current quantity: {updated.quantity}");
        }
        else
        {
            var newItem = new Item { id = _id, quantity = 1 };
            ctx.Db.item.Insert(newItem);
            Log.Info($"[AddItem] Inserted new item with ID {_id}");
        }
    }

    [Reducer]
    public static void DeleteItem(ReducerContext ctx, uint _id)
    {
        Log.Info($"[DeleteItem] Deleting item with ID {_id}");

        var entity = ctx.Db.item.id.Find(_id) ?? throw new Exception($"[DeleteItem] Item {_id} does not exist!");

        if (entity.quantity > 1)
        {
            var updated = entity;
            updated.quantity--;
            ctx.Db.item.id.Update(updated);
            Log.Info($"[DeleteItem] Decreased quantity of item {_id}. Current quantity: {updated.quantity}");
        }
        else
        {
            ctx.Db.item.id.Delete(entity.id);
            Log.Info($"[DeleteItem] Item {_id} fully removed!");
        }
    }

    /////////////////////////////////////////////////////////////
    /// PLAYER UPDATES

    [Reducer]
    public static void EnterGame(ReducerContext ctx, string name)
    {
        var player = ctx.Db.player.identity.Find(ctx.Sender) ?? throw new Exception("[EnterGame] Player not found");
        player.name = name;
        ctx.Db.player.identity.Update(player);
    }

    [Reducer]
    public static void EquipWeapon(ReducerContext ctx, uint itemId)
    {
        var player = ctx.Db.player.identity.Find(ctx.Sender) ?? throw new Exception("[EquipWeapon] Player not found");
        player.weapon = new Item { id = itemId, quantity = 1 };
        ctx.Db.player.identity.Update(player);
    }

    [Reducer]
    public static void EquipArmor(ReducerContext ctx, uint itemId)
    {
        var player = ctx.Db.player.identity.Find(ctx.Sender) ?? throw new Exception("[EquipArmor] Player not found");
        player.armor = new Item { id = itemId, quantity = 1 };
        ctx.Db.player.identity.Update(player);
    }

    [Reducer]
    public static void AddConsumableToPlayer(ReducerContext ctx, uint itemId)
    {
        var player = ctx.Db.player.identity.Find(ctx.Sender) ?? throw new Exception("[AddConsumableToPlayer] Player not found");
        if (player.consumables == null)
            player.consumables = new List<Item>();

        player.consumables.Add(new Item { id = itemId, quantity = 1 });
        ctx.Db.player.identity.Update(player);
    }

    /////////////////////////////////////////////////////////////
    /// STATION & TRAIN

    [Reducer]
    public static void AddTrain(ReducerContext ctx, string _id, string _from_station_id, string _to_station_id, uint _money)
    {
        var train = new Train { id = _id, from_station_id = _from_station_id, to_station_id = _to_station_id, money = _money };
        ctx.Db.train.Insert(train);
    }

    [Reducer]
    public static void DeleteTrain(ReducerContext ctx, string _id)
    {
        var train = ctx.Db.train.id.Find(_id) ?? throw new Exception($"[DeleteTrain] Train {_id} does not exist!");
        ctx.Db.train.id.Delete(train.id);
    }

    [Reducer]
    public static void AddStation(ReducerContext ctx, string _id, string _name)
    {
        var station = new Station { id = _id, name = _name, items = new List<Item>() };
        ctx.Db.station.Insert(station);
    }

    [Reducer]
    public static void DeleteStation(ReducerContext ctx, string _id)
    {
        var station = ctx.Db.station.id.Find(_id) ?? throw new Exception($"[DeleteStation] Station {_id} does not exist!");
        ctx.Db.station.id.Delete(station.id);
    }
}
