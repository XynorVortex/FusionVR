// Updated to fusion 2 by coolpuppykid
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Photon.Voice.Fusion;

namespace Fusion.VR
{
    public class FusionVRManager : MonoBehaviour
    {
        public static FusionVRManager Manager { get; private set; }

        [Header("App IDs")]
        public string FusionAppId;
        public string VoiceAppId;

        [Header("Editor Helpers")]
        public bool hideInfo = false;

        [Header("Photon")]
        [Tooltip("Please read https://doc.photonengine.com/en-us/pun/current/connection-and-authentication/regions for more information.\nLeave this empty to default to the nearest region for the player.")]
        public string Region = "eu";

        [Header("Player")]
        public Transform Head;
        public Transform LeftHand;
        public Transform RightHand;

        [Header("Networking")]
        public string DefaultQueue = "Default";
        public int DefaultRoomLimit = 100;
        public GameMode NetworkingMode = GameMode.Shared;
        public GameObject VoiceAndRunner;

        [Header("Other")]
        [Tooltip("If the user shall connect when this object has awoken")]
        public bool ConnectOnAwake = true;
        [Tooltip("If the user shall join a room when they connect")]
        public bool JoinRoomOnConnect = true;

        [NonSerialized] public NetworkRunner Runner;
        [NonSerialized] public FusionVoiceClient VoiceClient;

        [NonSerialized] public NetworkSceneManagerDefault SceneManagerInstance;
        [NonSerialized] public string CurrentLobby = "None";

        public static Action<NetworkRunner> OnHostMigrationResume;

        private void Start()
        {
            if (Manager == null)
                Manager = this;
            else
            {
                Debug.LogError("There can't be multiple PhotonVRManagers in a scene");
                Application.Quit();
            }

            DontDestroyOnLoad(Head.root);
            DontDestroyOnLoad(gameObject);

            if (ConnectOnAwake)
                Connect();
        }

#if UNITY_EDITOR
        public void CheckDefaultValues()
        {
            bool b = CheckForRig(this);
            if (b)
            {
                FusionAppId = Photon.Realtime.PhotonAppSettings.Global.AppSettings.AppIdFusion;
                VoiceAppId = Photon.Realtime.PhotonAppSettings.Global.AppSettings.AppIdVoice;
                Debug.Log("Attempted to set default values");
            }
        }

        private bool CheckForRig(FusionVRManager manager)
        {
            GameObject[] objects = FindObjectsOfType<GameObject>();
            bool b = false;

            if (manager.Head == null)
            {
                b = true;
                foreach (GameObject obj in objects)
                {
                    if (obj.name.Contains("Camera") || obj.name.Contains("Head"))
                    {
                        manager.Head = obj.transform;
                        break;
                    }
                }
            }

            if (manager.LeftHand == null)
            {
                b = true;
                foreach (GameObject obj in objects)
                {
                    if (obj.name.Contains("Left") && (obj.name.Contains("Hand") || obj.name.Contains("Controller")))
                    {
                        manager.LeftHand = obj.transform;
                        break;
                    }
                }
            }

            if (manager.RightHand == null)
            {
                b = true;
                foreach (GameObject obj in objects)
                {
                    if (obj.name.Contains("Right") && (obj.name.Contains("Hand") || obj.name.Contains("Controller")))
                    {
                        manager.RightHand = obj.transform;
                        break;
                    }
                }
            }

            return b;
        }
#endif

        public static bool Connect()
        {
            if (Manager.Runner != null)
            {
                Debug.LogError("Already connected to server");
                return false;
            }

            if (string.IsNullOrEmpty(Manager.FusionAppId))
            {
                Debug.LogError("Please input an app id");
                return false;
            }

            GameObject voiceAndRunner = Instantiate(Manager.VoiceAndRunner);

            NetworkProjectConfig.Global.HostMigration.EnableAutoUpdate = true;
            NetworkProjectConfig.Global.HostMigration.UpdateDelay = 5;
            Photon.Realtime.PhotonAppSettings.Global.AppSettings.AppIdFusion = Manager.FusionAppId;
            Photon.Realtime.PhotonAppSettings.Global.AppSettings.AppIdVoice = Manager.VoiceAppId;

            if (!string.IsNullOrEmpty(Manager.Region))
                Photon.Realtime.PhotonAppSettings.Global.AppSettings.FixedRegion = Manager.Region;

            Manager.Runner = voiceAndRunner.GetComponent<NetworkRunner>();
            Manager.Runner.ProvideInput = true;

            if (!string.IsNullOrEmpty(Manager.VoiceAppId))
                Manager.VoiceClient = voiceAndRunner.GetComponent<FusionVoiceClient>();

            if (Manager.JoinRoomOnConnect)
                JoinRandomRoom(Manager.DefaultQueue, Manager.DefaultRoomLimit);

            return true;
        }

        public static async Task<bool> JoinRandomRoom(string Queue, int MaxPlayers)
        {
            if (Manager.Runner == null)
                Connect();

            // Only add scene manager once
            if (Manager.SceneManagerInstance == null)
                Manager.SceneManagerInstance = Manager.gameObject.AddComponent<NetworkSceneManagerDefault>();

            Dictionary<string, SessionProperty> roomProperties = new Dictionary<string, SessionProperty>();
            roomProperties.Add("queue", Queue);
            roomProperties.Add("version", Application.version);

            Manager.Runner.ProvideInput = true;

            StartGameResult result = await Manager.Runner.StartGame(new StartGameArgs()
            {
                GameMode = Manager.NetworkingMode,
                SessionProperties = roomProperties,
                PlayerCount = MaxPlayers,
                SceneManager = Manager.SceneManagerInstance
            });

            if (!result.Ok)
            {
                Debug.LogError("Failed to join room");
                Manager.CurrentLobby = "None";
            }
            else
            {
                Debug.Log($"Joined a room: {Queue}");
                Manager.CurrentLobby = Queue;
            }

            return result.Ok;
        }

        public static void LeaveRoom()
        {
            if (Manager.Runner != null)
            {
                Manager.Runner.Shutdown(shutdownReason: ShutdownReason.Ok);
                Manager.CurrentLobby = "None";

                if (Manager.SceneManagerInstance != null)
                {
                    Destroy(Manager.SceneManagerInstance);
                    Manager.SceneManagerInstance = null;
                }
            }
        }

        public static bool Disconnect()
        {
            if (Manager.Runner != null)
                Manager.Runner.Disconnect(Manager.Runner.LocalPlayer);
            return true;
        }

        public static string GenerateRoomCode()
        {
            return new System.Random().Next(99999).ToString();
        }
    }
}
