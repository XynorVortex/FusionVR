#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Fusion.VR.Editor
{
    [CustomEditor(typeof(FusionVRManager))]
    public class FusionVRManagerGUI : UnityEditor.Editor
    {
        private static Texture2D logo;
        private string roomInput = "";

        public override void OnInspectorGUI()
        {
            FusionVRManager manager = (FusionVRManager)target;

            // Logo
            if (logo == null)
                logo = Resources.Load<Texture2D>("FusionVR/Assets/FusionVRLogoNoBackSmall");
            if (logo != null)
                GUILayout.Label(new GUIContent() { image = logo });

            GUILayout.Space(5);

            // Hide / Show button
            if (GUILayout.Button(manager.hideInfo ? "Show IDs" : "Hide IDs"))
                manager.hideInfo = !manager.hideInfo;

            GUILayout.Space(5);

            // Draw all properties, skipping hideInfo and replacing App IDs with "Hidden" if needed
            SerializedProperty prop = serializedObject.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (prop.name == "hideInfo")
                    continue; // skip checkbox

                if (prop.name == "FusionAppId" || prop.name == "VoiceAppId")
                {
                    if (manager.hideInfo)
                    {
                        EditorGUILayout.LabelField(prop.displayName + ": Hidden");
                        continue;
                    }
                }

                EditorGUILayout.PropertyField(prop, true);
            }

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(10);

            // Show current lobby
            EditorGUILayout.LabelField("Current Lobby:", manager.CurrentLobby);

            GUILayout.Space(10);

            // Room control buttons
            EditorGUILayout.LabelField("Room Control", EditorStyles.boldLabel);
            roomInput = EditorGUILayout.TextField("Room Name (optional)", roomInput);

            if (GUILayout.Button("Join Room"))
            {
                if (string.IsNullOrEmpty(roomInput))
                    FusionVRManager.JoinRandomRoom(manager.DefaultQueue, manager.DefaultRoomLimit);
                else
                    FusionVRManager.JoinRandomRoom(roomInput, manager.DefaultRoomLimit);
            }

            if (GUILayout.Button("Leave Room"))
                FusionVRManager.LeaveRoom();
        }
    }
}
#endif
