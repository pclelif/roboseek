using UnityEngine;
using UnityEngine.UI;
using Robot.Input;
using Robot.Combat;
using Robot.ObjectHunt;
using Robot.Player.Movement;
using Robot.Player.CameraControl;
using Robot.NPC;
namespace Robot.UI.Production
{
    public sealed class TutorialHintUI : MonoBehaviour
    {
        public CanvasGroup group;
        public Text key, message;
        public PlayerInputReader input;
        public RobotMovementController movement;
        public ThirdPersonCameraController cameraController;
        public CombatAttack attack;
        public ObjectHuntRoundManager hunt;
        public InteractionStateAdapter interaction;
        private static readonly string[] Keys = { "W A S D", "MOUSE", "E", "Q" };
        private static readonly string[] Messages = { "MOVE", "LOOK AROUND", "SCAN & RETRIEVE", "ATTACK" };
        private readonly bool[] done = new bool[4];
        private bool gameplay;
        private readonly System.Collections.Generic.HashSet<NpcRobotController> encounters = new System.Collections.Generic.HashSet<NpcRobotController>();
        private bool encounter
        {
            get
            {
                foreach (var npc in encounters)
                    if (npc != null && npc.isActiveAndEnabled && !npc.GetComponent<CombatHealth>().IsKnockedOut &&
                        (npc.transform.position - movement.transform.position).sqrMagnitude <= 36f) return true;
                return false;
            }
        }
        private Vector3 lastPosition;
        private int current = -1;
        private const string Prefix = "RobotHunt.Tutorial.v1.";
        private void OnEnable()
        {
            for (int i = 0; i < done.Length; i++) done[i] = PlayerPrefs.GetInt(Prefix + i, 0) == 1;
            lastPosition = movement.transform.position;
            cameraController.LookPerformed += OnLook; attack.AttackStarted += OnAttack;
            hunt.TargetCollected += OnRetrieved; interaction.Changed += OnInteractable;
            NpcRobotController.PlayerEncounterChanged += OnEncounter;
        }
        private void OnDisable()
        {
            cameraController.LookPerformed -= OnLook; attack.AttackStarted -= OnAttack;
            hunt.TargetCollected -= OnRetrieved; interaction.Changed -= OnInteractable;
            NpcRobotController.PlayerEncounterChanged -= OnEncounter;
        }
        public void SetGameplay(bool value) { gameplay = value; lastPosition = movement.transform.position; Refresh(); }
        private void LateUpdate()
        {
            var position = movement.transform.position;
            if (gameplay && !done[0] && movement.enabled && movement.ControlEnabled && input.MoveInput.sqrMagnitude > .1f && (position - lastPosition).sqrMagnitude > .00001f) Complete(0);
            lastPosition = position;
            Refresh();
        }
        private void OnLook() { if (gameplay) Complete(1); }
        private void OnAttack() { if (gameplay && encounter) Complete(3); }
        private void OnRetrieved(TargetDefinition target, int count) { Complete(2); }
        private void OnInteractable(CollectibleTarget target) => Refresh();
        private void OnEncounter(NpcRobotController npc, Transform player, bool active) { if (movement == null || player != movement.transform) return; if (active) encounters.Add(npc); else encounters.Remove(npc); Refresh(); }
        private void Complete(int index)
        {
            if (done[index]) return;
            done[index] = true; PlayerPrefs.SetInt(Prefix + index, 1); PlayerPrefs.Save(); Refresh();
        }
        private void Refresh()
        {
            if (group == null) return;
            current = interaction.Current != null ? 2 : encounter ? 3 : !done[0] ? 0 : !done[1] ? 1 : -1;
            // The contextual E prompt itself is the interaction lesson; never duplicate it.
            var health = movement.GetComponent<CombatHealth>();
            bool show = (health == null || !health.IsKnockedOut) && gameplay && current >= 0 && current != 2 && interaction.Current == null;
            group.alpha = show ? 1 : 0;
            if (!show) return;
            key.text = Keys[current];
            message.text = UILocalization.Translate(Messages[current]);
        }
    }
}
