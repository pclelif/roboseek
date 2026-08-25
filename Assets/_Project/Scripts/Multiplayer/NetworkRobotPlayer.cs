using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Robot.Combat;
using Robot.Input;
using Robot.Player.CameraControl;
using Robot.Player.Movement;
using Robot.Robots.Customization;

namespace Robot.Multiplayer
{
    [RequireComponent(typeof(NetworkObject), typeof(OwnerNetworkTransform))]
    public sealed class NetworkRobotPlayer : NetworkBehaviour
    {
        public enum PlayerState : byte { Connecting, Active, KnockedOut, Disconnected }

        private readonly NetworkVariable<int> colorIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<PlayerState> playerState = new NetworkVariable<PlayerState>(PlayerState.Connecting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<FixedString64Bytes> colorMessage = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

        private RobotColorCustomizer customizer;
        private CombatHealth health;
        public int ColorIndex => colorIndex.Value;
        public PlayerState State => playerState.Value;
        public string LastColorMessage => colorMessage.Value.ToString();
        public event Action<int> ColorChanged;
        public event Action<PlayerState> StateChanged;

        private void Awake()
        {
            customizer = GetComponent<RobotColorCustomizer>();
            health = GetComponent<CombatHealth>();
        }

        public override void OnNetworkSpawn()
        {
            colorIndex.OnValueChanged += HandleColorChanged;
            playerState.OnValueChanged += HandleStateChanged;
            if (IsServer && health != null)
            {
                health.KnockedOut += HandleKnockedOut;
                health.Recovered += HandleRecovered;
            }
            SetLocalAuthority(IsOwner);
            if (colorIndex.Value >= 0) ApplyColor(colorIndex.Value);

            if (IsServer) AssignColor(-1);
            if (IsOwner)
            {
                int preferred = RobotColorService.Instance != null ? RobotColorService.Instance.LoadSinglePlayerSelection() : 0;
                RequestColorRpc(preferred);
            }
        }

        public override void OnNetworkDespawn()
        {
            colorIndex.OnValueChanged -= HandleColorChanged;
            playerState.OnValueChanged -= HandleStateChanged;
            if (health != null)
            {
                health.KnockedOut -= HandleKnockedOut;
                health.Recovered -= HandleRecovered;
            }
            if (IsServer && RobotColorService.Instance != null) RobotColorService.Instance.Release(OwnerClientId);
        }

        [Rpc(SendTo.Server)]
        public void RequestColorRpc(int requestedIndex)
        {
            AssignColor(requestedIndex);
        }

        public void SetStateServer(PlayerState value)
        {
            if (IsServer) playerState.Value = value;
        }

        private void AssignColor(int requestedIndex)
        {
            if (!IsServer || RobotColorService.Instance == null) return;
            bool exact = RobotColorService.Instance.TryReserve(OwnerClientId, requestedIndex, out int assigned, out string message);
            colorIndex.Value = assigned;
            colorMessage.Value = new FixedString64Bytes(message);
            playerState.Value = PlayerState.Active;
            if (!exact && !string.IsNullOrEmpty(message)) Debug.LogWarning($"[Network Color] Client {OwnerClientId}: {message}; assigned index {assigned}.");
        }

        private void SetLocalAuthority(bool localOwner)
        {
            RobotMovementController movement = GetComponent<RobotMovementController>();
            PlayerInputReader input = GetComponent<PlayerInputReader>();
            PlayerCombatInput combatInput = GetComponent<PlayerCombatInput>();
            CharacterController cc = GetComponent<CharacterController>();
            Robot.UI.HUD.RobotShowcaseUI showcase = GetComponent<Robot.UI.HUD.RobotShowcaseUI>();

            if (cc != null)
            {
                // Crucial for NGO: Disable CharacterController on non-owners so NetworkTransform can interpolate positions smoothly without fighting physics
                cc.enabled = localOwner;
            }

            if (movement != null)
            {
                movement.SetControlEnabled(localOwner);
                if (localOwner && Camera.main != null)
                {
                    movement.SetCameraTransform(Camera.main.transform);
                }
            }

            if (input != null) input.enabled = localOwner;
            if (combatInput != null) combatInput.enabled = localOwner;
            if (showcase != null) showcase.enabled = localOwner;

            if (localOwner)
            {
                ThirdPersonCameraController cameraController = UnityEngine.Object.FindFirstObjectByType<ThirdPersonCameraController>();
                if (cameraController != null)
                {
                    cameraController.SetTarget(transform);
                }
            }
        }

        [Rpc(SendTo.Server)]
        public void TriggerAttackServerRpc()
        {
            TriggerAttackClientRpc();
        }

        [Rpc(SendTo.NotServer)]
        private void TriggerAttackClientRpc()
        {
            if (!IsOwner)
            {
                Robot.Player.RobotAnimator anim = GetComponent<Robot.Player.RobotAnimator>();
                if (anim != null) anim.PlayAttack();
            }
        }

        private void HandleColorChanged(int _, int current) { ApplyColor(current); ColorChanged?.Invoke(current); }
        private void HandleStateChanged(PlayerState _, PlayerState current) => StateChanged?.Invoke(current);
        private void ApplyColor(int index) { if (customizer != null) customizer.ApplyPaletteIndex(index); }
        private void HandleKnockedOut(CombatHealth _) { if (IsServer) playerState.Value = PlayerState.KnockedOut; }
        private void HandleRecovered(CombatHealth _) { if (IsServer) playerState.Value = PlayerState.Active; }
    }
}
