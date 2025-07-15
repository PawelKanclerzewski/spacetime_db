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

    [Table(Name = "item", Public = true)]
    public partial struct Item
    {
        [PrimaryKey, AutoInc]
        public uint id;
        public string name;
        public uint quantity;
    }

    [Table(Name = "armor", Public = true)]
    public partial struct Armor
    {
        [PrimaryKey, AutoInc]
        public uint id;
        public string name;
        public uint defence;
    }

    [Table(Name = "weapon", Public = true)]
    public partial struct Weapon
    {
        [PrimaryKey, AutoInc]
        public uint id;
        public string name;
        public uint attack;
    }

    [Table(Name = "consumables", Public = true)]
    public partial struct Consumables
    {
        [PrimaryKey, AutoInc]
        public uint id;
        public string name;
        public uint value;
    }

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
        public List<Item> items;
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
        if (player != null)
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
                items = new List<Item>()
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
    /// INSERTING ENTITIES TO TABLES

    [Reducer]
    public static void AddItem(ReducerContext ctx, uint _id, string _name)
    {
        Log.Info($"[AddItem] Adding item with ID {_id}, name {_name}");
        var entity = ctx.Db.item.id.Find(_id);
        if (entity != null)
        {
            entity.quantity++;
            ctx.Db.item.id.Update(entity);
            Log.Info($"[AddItem] Increased quantity of item {_id}. Current quantity: {entity.quantity}");
        }
        else
        {
            var newItem = new Item { id = _id, name = _name, quantity = 1 };
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
            entity.quantity--;
            ctx.Db.item.id.Update(entity);
            Log.Info($"[DeleteItem] Decreased quantity of item {_id}. Current quantity: {entity.quantity}");
        }
        else
        {
            ctx.Db.item.id.Delete(entity);
            Log.Info($"[DeleteItem] Item {_id} fully removed!");
        }
    }

    [Reducer]
    public static void AddTrain(ReducerContext ctx, string _id, string _from_station_id, string _to_station_id, uint _money)
    {
        Log.Info($"[AddTrain] Adding train {_id} from {_from_station_id} to {_to_station_id} with money {_money}");
        var train = new Train
        {
            id = _id,
            from_station_id = _from_station_id,
            to_station_id = _to_station_id,
            money = _money
        };
        ctx.Db.train.Insert(train);
        Log.Info($"[AddTrain] Train {_id} added.");
    }

    [Reducer]
    public static void DeleteTrain(ReducerContext ctx, string _id)
    {
        Log.Info($"[DeleteTrain] Deleting train {_id}");
        var entity = ctx.Db.train.identity.Find(_id) ?? throw new Exception($"[DeleteTrain] Train {_id} does not exist!");
        ctx.Db.train.identity.Delete(entity);
        Log.Info($"[DeleteTrain] Train {_id} deleted.");
    }

    [Reducer]
    public static void AddStation(ReducerContext ctx, string _id, string _name)
    {
        Log.Info($"[AddStation] Adding station {_id} with name {_name}");
        var station = new Station
        {
            id = _id,
            name = _name,
            items = new List<Item>()
        };
        ctx.Db.station.Insert(station);
        Log.Info($"[AddStation] Station {_id} added.");
    }

    [Reducer]
    public static void DeleteStation(ReducerContext ctx, string _id)
    {
        Log.Info($"[DeleteStation] Deleting station {_id}");
        var entity = ctx.Db.station.identity.Find(_id) ?? throw new Exception($"[DeleteStation] Station {_id} does not exist!");
        ctx.Db.station.identity.Delete(entity);
        Log.Info($"[DeleteStation] Station {_id} deleted.");
    }

    [Reducer]
    public static void AddWeapon(ReducerContext ctx, string name, uint attack)
    {
        Log.Info($"[AddWeapon] Adding weapon {name} with attack {attack}");
        var weapon = new Weapon { name = name, attack = attack };
        ctx.Db.weapon.Insert(weapon);
        AddItem(ctx, weapon.id, weapon.name);
        Log.Info($"[AddWeapon] Weapon {name} added with ID {weapon.id}.");
    }

    [Reducer]
    public static void DeleteWeapon(ReducerContext ctx, uint id)
    {
        Log.Info($"[DeleteWeapon] Deleting weapon with ID {id}");
        var weapon = ctx.Db.weapon.id.Find(id) ?? throw new Exception($"[DeleteWeapon] Weapon ID {id} not found!");
        ctx.Db.weapon.id.Delete(weapon);
        DeleteItem(ctx, id);
        Log.Info($"[DeleteWeapon] Weapon with ID {id} deleted.");
    }

    [Reducer]
    public static void AddArmor(ReducerContext ctx, string name, uint defence)
    {
        Log.Info($"[AddArmor] Adding armor {name} with defence {defence}");
        var armor = new Armor { name = name, defence = defence };
        ctx.Db.armor.Insert(armor);
        AddItem(ctx, armor.id, armor.name);
        Log.Info($"[AddArmor] Armor {name} added with ID {armor.id}.");
    }

    [Reducer]
    public static void DeleteArmor(ReducerContext ctx, uint id)
    {
        Log.Info($"[DeleteArmor] Deleting armor with ID {id}");
        var armor = ctx.Db.armor.id.Find(id) ?? throw new Exception($"[DeleteArmor] Armor ID {id} not found!");
        ctx.Db.armor.id.Delete(armor);
        DeleteItem(ctx, id);
        Log.Info($"[DeleteArmor] Armor with ID {id} deleted.");
    }

    [Reducer]
    public static void AddConsumable(ReducerContext ctx, string name, uint value)
    {
        Log.Info($"[AddConsumable] Adding consumable {name} with value {value}");
        var consumable = new Consumables { name = name, value = value };
        ctx.Db.consumables.Insert(consumable);
        AddItem(ctx, consumable.id, consumable.name);
        Log.Info($"[AddConsumable] Consumable {name} added with ID {consumable.id}.");
    }

    [Reducer]
    public static void DeleteConsumable(ReducerContext ctx, uint id)
    {
        Log.Info($"[DeleteConsumable] Deleting consumable with ID {id}");
        var consumable = ctx.Db.consumables.id.Find(id) ?? throw new Exception($"[DeleteConsumable] Consumable ID {id} not found!");
        ctx.Db.consumables.id.Delete(consumable);
        DeleteItem(ctx, id);
        Log.Info($"[DeleteConsumable] Consumable with ID {id} deleted.");
    }

    [Reducer]
    public static void EnterGame(ReducerContext ctx, string name)
    {
        Log.Info($"[EnterGame] Setting player name to {name}");
        var player = ctx.Db.player.identity.Find(ctx.Sender) ?? throw new Exception("[EnterGame] Player not found");
        player.name = name;
        ctx.Db.player.identity.Update(player);
        Log.Info($"[EnterGame] Player name set to {name}.");
    }
    
    [Reducer]
    public static void AddExistingItemToPlayer(ReducerContext ctx, Identity playerIdentity, uint itemId)
    {
        Log.Info($"[AddExistingItemToPlayer] Adding item ID {itemId} to player {playerIdentity}");

        var player = ctx.Db.player.identity.Find(playerIdentity) ?? throw new Exception($"[AddExistingItemToPlayer] Player {playerIdentity} not found!");

        if (player.items == null)
            player.items = new List<Item>();
        var globalItem = ctx.Db.item.id.Find(itemId) ?? throw new Exception($"[AddExistingItemToPlayer] Global item {itemId} not found!");
        player.items.Add(globalItem);
        Log.Info($"[AddExistingItemToPlayer] Added new item {itemId} to player");
        ctx.Db.player.identity.Update(player);
    }
}
