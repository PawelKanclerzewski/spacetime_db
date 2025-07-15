using UnityEngine;
using System;
using System.Collections.Generic;
using SpacetimeDB;
using SpacetimeDB.Types;

public class GameManager : MonoBehaviour
{
    const string SERVER_URL = "http://127.0.0.1:3000";
    const string MODULE_NAME = "spacetime-db-service";

    public static event Action OnConnected;
    public static event Action OnSubscriptionApplied;

    public static GameManager Instance { get; private set; }
    public static Identity LocalIdentity { get; private set; }
    public static DbConnection Conn { get; private set; }

    // Wygodne property do wywołań reducerów z innych miejsc
    public static dynamic Reducers => Conn?.Reducers;

    void Start()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        var builder = DbConnection.Builder()
            .OnConnect(HandleConnect)
            .OnConnectError(HandleConnectError)
            .OnDisconnect(HandleDisconnect)
            .WithUri(SERVER_URL)
            .WithModuleName(MODULE_NAME);

        if (PlayerPrefs.HasKey(AuthToken.Token))
        {
            builder = builder.WithToken(AuthToken.Token);
        }

        Conn = builder.Build();
    }

    void HandleConnect(DbConnection _conn, Identity identity, string token)
    {
        Debug.Log("Connected.");
        AuthToken.SaveToken(token);
        LocalIdentity = identity;
        OnConnected?.Invoke();

        Conn.SubscriptionBuilder()
            .OnApplied(HandleSubscriptionApplied)
            .SubscribeToAllTables();
    }

    void HandleConnectError(Exception ex)
    {
        Debug.Log($"Connection error: {ex}");
    }

    void HandleDisconnect(DbConnection _conn, Exception ex)
    {
        Debug.Log("Disconnected.");
        if (ex != null)
        {
            Debug.LogException(ex);
        }
    }

    private void HandleSubscriptionApplied(SubscriptionEventContext ctx)
    {
        Debug.Log("Subscription applied!");
        OnSubscriptionApplied?.Invoke();

    }

    public static bool IsConnected()
    {
        return Conn != null && Conn.IsActive;
    }

    public void Disconnect()
    {
        Conn.Disconnect();
        Conn = null;
    }

    void Update()
    {
        // Your update code here
    }

    // --- Metody do wywoływania reducerów poprzez Conn.Reducers ---

    // ADD methods
    public void AddItem(uint id, string name)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.AddItem(id, name);
    }

    public void AddTrain(string id, string fromStationId, string toStationId, uint money)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.AddTrain(id, fromStationId, toStationId, money);
    }

    public void AddStation(string id, string name)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.AddStation(id, name);
    }

    public void AddWeapon(string name, uint attack)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.AddWeapon(name, attack);
    }

    public void AddArmor(string name, uint defence)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.AddArmor(name, defence);
    }

    public void AddConsumable(string name, uint value)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.AddConsumable(name, value);
    }

    public void AddExistingItemToPlayer(Identity playerIdentity, uint itemId)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.AddExistingItemToPlayer(playerIdentity, itemId);
    }

    // DELETE methods
    public void DeleteItem(uint id)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.DeleteItem(id);
    }

    public void DeleteTrain(string id)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.DeleteTrain(id);
    }

    public void DeleteStation(string id)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.DeleteStation(id);
    }

    public void DeleteWeapon(uint id)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.DeleteWeapon(id);
    }

    public void DeleteArmor(uint id)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.DeleteArmor(id);
    }

    public void DeleteConsumable(uint id)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.DeleteConsumable(id);
    }

    // Example: EnterGame reducer invocation
    public void EnterGame(string playerName)
    {
        if (!IsConnected() || Reducers == null) return;
        Reducers.EnterGame(playerName);
    }
}
