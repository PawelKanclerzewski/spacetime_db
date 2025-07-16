using UnityEngine;
using System;
using SpacetimeDB;
using SpacetimeDB.Types;

public class GameManager : MonoBehaviour
{
    // Server configuration
    const string SERVER_URL = "http://127.0.0.1:3000";
    const string MODULE_NAME = "spacetime-db-service";

    // Stores local player's identity
    public static Identity LocalIdentity { get; private set; }

    // Main connection object to SpacetimeDB
    public static DbConnection Conn { get; private set; }

    void Start()
    {
        // Set target frame rate for smoother gameplay
        Application.targetFrameRate = 60;

        // Initialize database connection builder with handlers
        var builder = DbConnection.Builder()
            .OnConnect(HandleConnect)
            .OnConnectError(HandleConnectError)
            .OnDisconnect(HandleDisconnect)
            .WithUri(SERVER_URL)
            .WithModuleName(MODULE_NAME);

        // Add authentication token if it exists
        if (PlayerPrefs.HasKey(AuthToken.Token))
        {
            builder = builder.WithToken(AuthToken.Token);
        }

        // Build and assign the database connection
        Conn = builder.Build();
    }

    // Flag to indicate waiting for item to be available
    private bool waitingForItem = false;

    void Update()
    {
        // On pressing Enter, send reducer to add an item
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("[GameManager] Enter key pressed — sending AddItem reducer");
            Conn.Reducers.AddItem(11, "Miecz");
            waitingForItem = true;
        }

        // If waiting for item, check if it's available in DB and log it
        if (waitingForItem)
        {

            // Try to find the specific item with ID 11
            var item = Conn.Db.Item.Id.Find(11);
            if (item != null)
            {
                Debug.Log($"[GameManager] Item received: {item.Name}, qty: {item.Quantity}");
                waitingForItem = false;
            }
        }
    }

    // Called when connection is successfully established
    void HandleConnect(DbConnection conn, Identity identity, string token)
    {
        Debug.Log("[GameManager] Connected to SpacetimeDB.");

        // Save authentication token
        AuthToken.SaveToken(token);

        // Store identity of connected player
        LocalIdentity = identity;

        // Subscribe to all tables in the database
        Conn.SubscriptionBuilder()
            .OnApplied(HandleSubscriptionApplied)
            .SubscribeToAllTables();
    }

    // Called when subscription to tables is successfully applied
    private void HandleSubscriptionApplied(SubscriptionEventContext ctx)
    {
        Debug.Log("Subscription applied!");
    }

    // Called when connection attempt fails
    void HandleConnectError(Exception ex)
    {
        Debug.LogError($"[GameManager] Connection error: {ex}");
    }

    // Called when disconnected from the server
    void HandleDisconnect(DbConnection conn, Exception ex)
    {
        Debug.LogWarning("[GameManager] Disconnected.");
        if (ex != null)
        {
            Debug.LogException(ex);
        }
    }
}
