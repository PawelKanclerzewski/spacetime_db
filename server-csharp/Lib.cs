using SpacetimeDB;

public static partial class Module
{
    /////////////////////////////////////////////////////////////
    /// Tables

    // Train entity with departure/arrival station and money value
    [Table(Name = "train", Public = true)]
    public partial struct Train
    {
        [PrimaryKey]
        public string id;
        public string from_station_id;
        public string to_station_id;
        public uint money;
    }

    // Station entity containing a list of items
    [Table(Name = "station", Public = true)]
    public partial struct Station
    {
        [PrimaryKey]
        public string id;
        public string name;
        public List<Item> items;
    }

    // Basic item with quantity
    [Table(Name = "item", Public = true)]
    public partial struct Item
    {
        [PrimaryKey, AutoInc]
        public uint id;
        public string name;
        public uint quantity;
    }

    // Armor entity with defense value
    [Table(Name = "armor", Public = true)]
    public partial struct Armor
    {
        [PrimaryKey, AutoInc]
        public uint id;
        public string name;
        public uint defence;
    }

    // Weapon entity with attack value
    [Table(Name = "weapon", Public = true)]
    public partial struct Weapon
    {
        [PrimaryKey, AutoInc]
        public uint id;
        public string name;
        public uint attack;
    }

    // Consumable item with value (e.g. healing, buffs)
    [Table(Name = "consumables", Public = true)]
    public partial struct Consumables
    {
        [PrimaryKey, AutoInc]
        public uint id;
        public string name;
        public uint value;
    }

    // Player structure with inventory and money
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

    // Called when the database is initialized
    [Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        Log.Info($"[Init] Initializing...");
    }

    // Called when a client connects
    [Reducer(ReducerKind.ClientConnected)]
    public static void Connect(ReducerContext ctx)
    {
        Log.Info($"[Connect] Client connected: {ctx.Sender}");

        // If player is found in logged-out table, move them back to active players
        var player = ctx.Db.logged_out_player.identity.Find(ctx.Sender);
        if (player.HasValue)
        {
            Log.Info($"[Connect] Found logged out player, moving to active players.");
            ctx.Db.player.Insert(player.Value);
            ctx.Db.logged_out_player.identity.Delete(player.Value.identity);
        }
        else
        {
            // Otherwise, create a new player
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

    // Called when a client disconnects
    [Reducer(ReducerKind.ClientDisconnected)]
    public static void Disconnect(ReducerContext ctx)
    {
        Log.Info($"[Disconnect] Client disconnected: {ctx.Sender}");

        // Move player to logged_out_player table
        var player = ctx.Db.player.identity.Find(ctx.Sender) ?? throw new Exception("[Disconnect] Player not found");
        ctx.Db.logged_out_player.Insert(player);
        ctx.Db.player.identity.Delete(player.identity);
        Log.Info($"[Disconnect] Player moved to logged_out_player.");
    }

    /////////////////////////////////////////////////////////////
    /// INSERTING ENTITIES TO TABLES

    // Adds an item or increases its quantity if it already exists
    [Reducer]
    public static void AddItem(ReducerContext ctx, uint _id, string _name)
    {
        Log.Info($"[AddItem] Adding item with ID {_id}, name {_name}");

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
            var newItem = new Item { id = _id, name = _name, quantity = 1 };
            ctx.Db.item.Insert(newItem);
            Log.Info($"[AddItem] Inserted new item with ID {_id}");
        }
    }

    // Deletes or decreases quantity of an item
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

    // Adds a train between two stations
    [Reducer]
    public static void AddTrain(ReducerContext ctx, string _id, string _from_station_id, string _to_station_id, uint _money)
    {
        var train = new Train { id = _id, from_station_id = _from_station_id, to_station_id = _to_station_id, money = _money };
        ctx.Db.train.Insert(train);
    }

    // Deletes a train
    [Reducer]
    public static void DeleteTrain(ReducerContext ctx, string _id)
    {
        var train = ctx.Db.train.id.Find(_id) ?? throw new Exception($"[DeleteTrain] Train {_id} does not exist!");
        ctx.Db.train.id.Delete(train.id);
    }

    // Adds a new station
    [Reducer]
    public static void AddStation(ReducerContext ctx, string _id, string _name)
    {
        var station = new Station { id = _id, name = _name, items = new List<Item>() };
        ctx.Db.station.Insert(station);
    }

    // Deletes a station
    [Reducer]
    public static void DeleteStation(ReducerContext ctx, string _id)
    {
        var station = ctx.Db.station.id.Find(_id) ?? throw new Exception($"[DeleteStation] Station {_id} does not exist!");
        ctx.Db.station.id.Delete(station.id);
    }

    // Adds a weapon and registers it as an item
    [Reducer]
    public static void AddWeapon(ReducerContext ctx, string name, uint attack)
    {
        var weapon = new Weapon { name = name, attack = attack };
        ctx.Db.weapon.Insert(weapon);
        AddItem(ctx, weapon.id, weapon.name);
    }

    // Deletes a weapon and corresponding item
    [Reducer]
    public static void DeleteWeapon(ReducerContext ctx, uint id)
    {
        var weapon = ctx.Db.weapon.id.Find(id) ?? throw new Exception($"[DeleteWeapon] Weapon ID {id} not found!");
        ctx.Db.weapon.id.Delete(weapon.id);
        DeleteItem(ctx, id);
    }

    // Adds an armor and registers it as an item
    [Reducer]
    public static void AddArmor(ReducerContext ctx, string name, uint defence)
    {
        var armor = new Armor { name = name, defence = defence };
        ctx.Db.armor.Insert(armor);
        AddItem(ctx, armor.id, armor.name);
    }

    // Deletes an armor and corresponding item
    [Reducer]
    public static void DeleteArmor(ReducerContext ctx, uint id)
    {
        var armor = ctx.Db.armor.id.Find(id) ?? throw new Exception($"[DeleteArmor] Armor ID {id} not found!");
        ctx.Db.armor.id.Delete(armor.id);
        DeleteItem(ctx, id);
    }

    // Adds a consumable and registers it as an item
    [Reducer]
    public static void AddConsumable(ReducerContext ctx, string name, uint value)
    {
        var consumable = new Consumables { name = name, value = value };
        ctx.Db.consumables.Insert(consumable);
        AddItem(ctx, consumable.id, consumable.name);
    }

    // Deletes a consumable and corresponding item
    [Reducer]
    public static void DeleteConsumable(ReducerContext ctx, uint id)
    {
        var consumable = ctx.Db.consumables.id.Find(id) ?? throw new Exception($"[DeleteConsumable] Consumable ID {id} not found!");
        ctx.Db.consumables.id.Delete(consumable.id);
        DeleteItem(ctx, id);
    }

    // Sets the player's name on game entry
    [Reducer]
    public static void EnterGame(ReducerContext ctx, string name)
    {
        var player = ctx.Db.player.identity.Find(ctx.Sender) ?? throw new Exception("[EnterGame] Player not found");
        player.name = name;
        ctx.Db.player.identity.Update(player);
    }

    // Adds an existing item from global table to the player's inventory
    [Reducer]
    public static void AddExistingItemToPlayer(ReducerContext ctx, Identity playerIdentity, uint itemId)
    {
        var player = ctx.Db.player.identity.Find(playerIdentity) ?? throw new Exception($"[AddExistingItemToPlayer] Player {playerIdentity} not found!");
        var globalItem = ctx.Db.item.id.Find(itemId) ?? throw new Exception($"[AddExistingItemToPlayer] Global item {itemId} not found!");

        if (player.items == null)
            player.items = new List<Item>();

        player.items.Add(globalItem);
        ctx.Db.player.identity.Update(player);
    }
}
