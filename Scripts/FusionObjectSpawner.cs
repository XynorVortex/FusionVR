using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

[Serializable]
public class SpawnableObject
{
    public NetworkPrefabRef prefab;
    public Vector3 spawnPosition = Vector3.zero;
    public bool dontDestroyOnLoad = false;
    public Transform parent;
    public bool parentDontDestroyOnLoad = false;
}
  
  [ScriptHelp(BackColor = ScriptHeaderBackColor.Sand)]
  [DisallowMultipleComponent]
public class FusionObjectSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Spawn Settings")]
    public List<SpawnableObject> objectsToSpawn = new List<SpawnableObject>();

    private NetworkRunner cachedRunner;
    private List<NetworkObject> spawnedObjects = new List<NetworkObject>();
    private bool hasSpawned = false;

    private void Start()
    {
        StartCoroutine(WaitForRunner());
    }

    private IEnumerator WaitForRunner()
    {
        while (Fusion.VR.FusionVRManager.Manager == null || Fusion.VR.FusionVRManager.Manager.Runner == null)
        {
            yield return null;
        }

        cachedRunner = Fusion.VR.FusionVRManager.Manager.Runner;
        cachedRunner.AddCallbacks(this);
        Debug.Log("ObjectSpawner registered with NetworkRunner");
    }

    private void SpawnAllObjects(NetworkRunner runner)
    {
        if (hasSpawned)
        {
            Debug.LogWarning("Objects already spawned, skipping");
            return;
        }

        foreach (var spawnData in objectsToSpawn)
        {
            if (spawnData.prefab.IsValid == false)
            {
                Debug.LogError("Prefab in spawn list is not assigned or invalid!");
                continue;
            }

            NetworkObject spawnedObject = runner.Spawn(
                spawnData.prefab, 
                spawnData.spawnPosition, 
                Quaternion.identity
            );

            if (spawnData.parent != null)
            {
                spawnedObject.transform.SetParent(spawnData.parent, true);
                Debug.Log($"Spawned {spawnData.prefab} and parented to {spawnData.parent.name}");

                if (spawnData.parentDontDestroyOnLoad)
                {
                    DontDestroyOnLoad(spawnData.parent.gameObject);
                    Debug.Log($"Set parent {spawnData.parent.name} to DontDestroyOnLoad");
                }
            }
            else
            {
                Debug.Log($"Spawned {spawnData.prefab} at {spawnData.spawnPosition}");
            }

            if (spawnData.dontDestroyOnLoad)
            {
                DontDestroyOnLoad(spawnedObject.gameObject);
                Debug.Log($"Set {spawnData.prefab} to DontDestroyOnLoad");
            }

            spawnedObjects.Add(spawnedObject);
        }

        hasSpawned = true;
        Debug.Log($"Spawned {spawnedObjects.Count} shared objects - everyone will see these synced by Fusion");
    }

    private void OnDestroy()
    {
        if (cachedRunner != null)
        {
            cachedRunner.RemoveCallbacks(this);
        }
    }

    // =====================================================================
    //                         FUSION 2 CALLBACKS
    // =====================================================================

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined: {player}, IsServer: {runner.IsServer}, GameMode: {runner.GameMode}");
        
        if (runner.IsServer || runner.GameMode == GameMode.Shared)
        {
            if (runner.ActivePlayers.Count() == 1)
            {
                Debug.Log("First player joined - spawning all objects");
                SpawnAllObjects(runner);
            }
            else
            {
                Debug.Log($"Player {player} joined - they will see the existing objects synced by Fusion");
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerLeft: {player}");
        
        if ((runner.IsServer || runner.GameMode == GameMode.Shared) && runner.ActivePlayers.Count() == 0)
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                {
                    runner.Despawn(obj);
                }
            }
            Debug.Log("Last player left - despawned all shared objects");
            spawnedObjects.Clear();
            hasSpawned = false;
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log($"OnConnectedToServer - IsServer: {runner.IsServer}, GameMode: {runner.GameMode}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log("Disconnected: " + reason);
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError("Connect failed: " + reason);
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        request.Accept();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList)
    {
        Debug.Log("Session list updated. Count = " + sessionList.Count);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        Debug.Log("Runner shutdown: " + reason);
        spawnedObjects.Clear();
        hasSpawned = false;
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }
}
