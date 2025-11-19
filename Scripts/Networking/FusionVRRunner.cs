using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

using Fusion;
using Photon.Voice;
using Photon.Voice.Fusion;
using Fusion.Sockets;
using Fusion.Photon.Realtime;

using Fusion.VR;
using Fusion.VR.Player;
using Fusion.VR.Cosmetics;
using Fusion.VR.Saving;
using Photon.Voice.Unity;

// WHY IS THIS FILE SO BIGGG
// k it was not that bad to fix

namespace Fusion.VR.Networking
{
    public class FusionVRRunner : MonoBehaviour, INetworkRunnerCallbacks
    {
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
        }

        private FusionVRNetworkedPlayerData lastData = new FusionVRNetworkedPlayerData();

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            FusionVRNetworkedPlayerData data = new FusionVRNetworkedPlayerData();

            // Head
            data.headPosition = FusionVRManager.Manager.Head.position;
            data.headRotation = FusionVRManager.Manager.Head.rotation;
            // Left hand
            data.leftHandPosition = FusionVRManager.Manager.LeftHand.position;
            data.leftHandRotation = FusionVRManager.Manager.LeftHand.rotation;

            // Right hand
            data.rightHandPosition = FusionVRManager.Manager.RightHand.position;
            data.rightHandRotation = FusionVRManager.Manager.RightHand.rotation;

            //Debug.Log(data.ToString());

            if (data != lastData)
            {
                //Debug.Log("Sending data");
                input.Set(data);
                lastData = data;
            }
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"Runner shutdown: {shutdownReason}");
            Destroy(gameObject);
        }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

        public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

            FusionVRManager.Connect();

            StartGameResult result = await FusionVRManager.Manager.Runner.StartGame(new StartGameArgs()
            {
                HostMigrationToken = hostMigrationToken,
                HostMigrationResume = Resume
            });
        }

        private void Resume(NetworkRunner runner) {}
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }
    }
}
