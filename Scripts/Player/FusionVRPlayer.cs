// Updated to fusion 2 by coolpuppykid you can remove this comment btw if you want

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Fusion;
using Fusion.VR;
using Fusion.VR.Cosmetics;

using TMPro;

namespace Fusion.VR.Player
{
    public class FusionVRPlayer : NetworkBehaviour
    {
        //thanks fusion docs https://doc.photonengine.com/fusion/current/getting-started/migration/coming-from-fusion-v1
        private ChangeDetector _changes;

        public static FusionVRPlayer localPlayer;

        public int PlayerId { get; private set; }

        [Header("Objects")]
        public Transform Head;
        public Transform Body;
        public Transform LeftHand;
        public Transform RightHand;

        [Header("Colour Objects")]
        public List<Renderer> renderers = new List<Renderer>();

        [Header("Networked Transforms")]
        public NetworkTransform HeadTransform;
        public NetworkTransform LeftHandTransform;
        public NetworkTransform RightHandTransform;

        [Header("Cosmetics")]
        public List<PlayerCosmeticSlot> cosmeticSlots = new List<PlayerCosmeticSlot>();

        [Header("Other")]
        public TextMeshPro NameText;
        public bool HideLocalName = true;
        public bool HideLocalPlayer = false;

        [Header("Networked Variables")] // i tried to make this networked but we need stupid regular variable
        public bool isLocalPlayer;  // Not exactly networked, but you can't make a stupid header without a regular stupid variable

        // we should enforce a character limit in the name cause we dont want a network overflow cause that would be very very bad
        [Networked, OnChangedRender(nameof(OnNickNameChanged))]
        public NetworkString<_32> NickName { get; set; } // I feel as if nobody is going to have their name over 32 characters, feel free to change it though
        [Networked, OnChangedRender(nameof(OnColourChanged))]
        public Color Colour { get; set; } 
        // coolpuppykid dont forget that below this comment is the Cosmetics networked tag :D
        [Networked, OnChangedRender(nameof(OnCosmeticsChanged)), Capacity(10)] // Default is max 10, because beyond that the game would probably start lagging
        public NetworkDictionary<NetworkString<_16>, NetworkString<_32>> Cosmetics => default;

        public override void Spawned()
        {
            // fusion 2 is mad confusing
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            if (Object.HasInputAuthority)
            {
                localPlayer = this;
                isLocalPlayer = true;
                Networking.PlayerSpawner.Instance.LoadPlayer();

                NameText.gameObject.SetActive(!HideLocalName);

                Head.gameObject.SetActive(!HideLocalPlayer);
                Body.gameObject.SetActive(!HideLocalPlayer);
                LeftHand.gameObject.SetActive(!HideLocalPlayer);
                RightHand.gameObject.SetActive(!HideLocalPlayer);
            }
        }

        private void Update()
        {
            // Check if this is the local player
            if (Object.HasInputAuthority)
            {
                // Move the objects locally
                // Head
                HeadTransform.transform.position = FusionVRManager.Manager.Head.position;
                HeadTransform.transform.rotation = FusionVRManager.Manager.Head.rotation;
                // Left hand
                LeftHandTransform.transform.position = FusionVRManager.Manager.LeftHand.position;
                LeftHandTransform.transform.rotation = FusionVRManager.Manager.LeftHand.rotation;
                // Right hand
                RightHandTransform.transform.position = FusionVRManager.Manager.RightHand.position;
                RightHandTransform.transform.rotation = FusionVRManager.Manager.RightHand.rotation;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out FusionVRNetworkedPlayerData data))
            {
                HeadTransform.Teleport(data.headPosition, data.headRotation);
                LeftHandTransform.Teleport(data.leftHandPosition, data.leftHandRotation);
                RightHandTransform.Teleport(data.rightHandPosition, data.rightHandRotation);

                //Debug.Log(data.ToString());
            }
        }

        public void OnNickNameChanged()
        {
            NameText.text = NickName.Value;
            gameObject.name = $"Player ({NickName.Value})";
            // this creates an error so lets try something else
            /*foreach (var change in _changes.DetectChanges(this))
            {
                switch (NickName)
                {
                    case nameof(NickName):
                        var current = NickName;
                        NameText.text = NickName.Value;
                        gameObject.name = $"Player ({NickName.Value})";
                        break;
                }
            }*/
        }

        public void OnColourChanged()
        {
            List<Renderer> renderers = this.renderers;
            foreach (Renderer renderer in renderers)
            {
               renderer.material.color = Colour;
            }
        }

        public void OnCosmeticsChanged()
        {
            // foreach? i was foreach once they locked me in a room a foreach room a foreach room with foreach`s the foreach`s make me crazy
            // pray to the lord that this works
            List<PlayerCosmeticSlot> slots = cosmeticSlots;

            // Foreach, foreach, foreach, foreach!! We love foreach!!
            foreach (KeyValuePair<NetworkString<_16>, NetworkString<_32>> cosmetic in Cosmetics)
            {
                foreach (PlayerCosmeticSlot slot in slots)
                {
                    if (cosmetic.Key == slot.SlotName)
                    {
                        foreach (Transform t in slot.Slot)
                        {
                            GameObject obj = t.gameObject;
                            obj.SetActive(obj.name == cosmetic.Value);

                            if (t.GetComponentInChildren<Collider>() != null)
                            {
                                Debug.LogWarning($"It is not recommended to have a collider on a cosmetic ({obj.name})");
                            }
                        }
                        break;
                    }
                }
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPCSetNickName(string name, RpcInfo info = default)
        {
            NickName = name;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPCSetColour(Color colour, RpcInfo info = default)
        {
            Colour = colour;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPCSetCosmetics(CosmeticSlot[] cosmetics, RpcInfo info = default)
        {
            int i = 0;
            foreach (CosmeticSlot cos in cosmetics)
            {
                if (i < Cosmetics.Capacity)
                {
                    Cosmetics.Set(cos.SlotName, cos.CosmeticName);
                }
            }
        }
    }
}