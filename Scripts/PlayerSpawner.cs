using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using Fusion.VR.Cosmetics;
using Fusion.VR.Saving;

namespace Fusion.VR.Networking
{
    public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static PlayerSpawner Instance { get; private set; }

        [Header("Player Settings")]
        public Color Colour = Color.black;
        [Tooltip("If left as nothing, there will be no default username")]
        public string DefaultUsername = "Player";

        [Header("Networking")]
        public NetworkPrefabRef NetworkedPlayerPrefab;

        [Header("Cosmetics")]
        public List<string> CosmeticSlots = new List<string>();

        [System.NonSerialized] public Dictionary<PlayerRef, NetworkObject> playerCache = new Dictionary<PlayerRef, NetworkObject>();
        [System.NonSerialized] public Dictionary<string, string> Cosmetics = new Dictionary<string, string>();

        private NetworkRunner cachedRunner;

        private void Start()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Debug.LogError("There can't be multiple PlayerSpawners in a scene");
                Destroy(gameObject);
                return;
            }

            if (string.IsNullOrEmpty(PlayerPrefs.GetString("Username")) && !string.IsNullOrEmpty(DefaultUsername))
                SetUsername(DefaultUsername + GenerateRoomCode());

            if (!string.IsNullOrEmpty(PlayerPrefs.GetString("Colour")))
                SetColour(JsonUtility.FromJson<Color>(PlayerPrefs.GetString("Colour")));

            if (!string.IsNullOrEmpty(PlayerPrefs.GetString("Cosmetics")))
                SetCosmetics(FusionVRPrefs.GetCosmetics(CosmeticSlots));

            if (Cosmetics == null)
                SetCosmetics(new Dictionary<string, string>());

            StartCoroutine(WaitForRunner());
        }

        private System.Collections.IEnumerator WaitForRunner()
        {
            while (FusionVRManager.Manager == null || FusionVRManager.Manager.Runner == null)
            {
                yield return null;
            }

            cachedRunner = FusionVRManager.Manager.Runner;
            cachedRunner.AddCallbacks(this);
            Debug.Log("PlayerSpawner registered with NetworkRunner");
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log("Player entered");

            if (!runner.IsRunning)
                return;

            bool shouldSpawn = true;

            switch (runner.GameMode)
            {
                case GameMode.AutoHostOrClient:
                case GameMode.Host:
                case GameMode.Client:
                    shouldSpawn = runner.IsServer;
                    break;
                case GameMode.Shared:
                    shouldSpawn = player == runner.LocalPlayer;
                    break;
                default:
                    break;
            }

            if (shouldSpawn)
            {
                Debug.Log("Spawning player");
                Vector3 spawnPosition = Vector3.zero;
                NetworkObject networkedPlayer = runner.Spawn(NetworkedPlayerPrefab, spawnPosition, Quaternion.identity, player);

                Debug.Log("Adding player to cache");
                playerCache.Add(player, networkedPlayer);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (playerCache.TryGetValue(player, out NetworkObject networkedPlayer))
            {
                runner.Despawn(networkedPlayer);
                playerCache.Remove(player);
            }
        }

        public void SetUsername(string Name)
        {
            int maxNameLenght = 32;
            if (Name.Length > maxNameLenght)
                Name = Name.Substring(0, maxNameLenght);

            if (Player.FusionVRPlayer.localPlayer != null && Player.FusionVRPlayer.localPlayer.NickName != Name)
                Player.FusionVRPlayer.localPlayer.RPCSetNickName(Name);

            PlayerPrefs.SetString("Username", Name);
            Debug.Log($"Set username to {Name}");
        }

        public void SetColour(Color PlayerColour)
        {
            Colour = PlayerColour;

            if (Player.FusionVRPlayer.localPlayer != null && Player.FusionVRPlayer.localPlayer.Colour != PlayerColour)
                Player.FusionVRPlayer.localPlayer.RPCSetColour(PlayerColour);

            PlayerPrefs.SetString("Colour", JsonUtility.ToJson(PlayerColour));
            Debug.Log($"Set colour to {JsonUtility.ToJson(PlayerColour)}");
        }

        public void SetCosmetics(Dictionary<string, string> PlayerCosmetics)
        {
            Cosmetics = PlayerCosmetics;

            if (Player.FusionVRPlayer.localPlayer != null)
                Player.FusionVRPlayer.localPlayer.RPCSetCosmetics(CosmeticSlot.CopyFrom(Cosmetics).ToArray());

            FusionVRPrefs.SaveCosmetics(Cosmetics);
            Debug.Log("Set cosmetics");
        }

        public static string GenerateRoomCode()
        {
            return new System.Random().Next(99999).ToString();
        }

        public void LoadPlayer()
        {
            SetUsername(PlayerPrefs.GetString("Username"));
            SetColour(Colour);
            SetCosmetics(Cosmetics);
        }

        private void OnDestroy()
        {
            if (cachedRunner != null)
            {
                cachedRunner.RemoveCallbacks(this);
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    }
}
