using UnityEngine;
using Robot.Core;
using Robot.Player.CameraControl;

namespace Robot.Player.Movement
{
    [DefaultExecutionOrder(20)]
    public sealed class MapTraversalSafety : MonoBehaviour
    {
        private MapLevelLayout layout;
        private RobotMovementController movement;
        private void Start() { layout = FindFirstObjectByType<MapLevelLayout>(); movement = GetComponent<RobotMovementController>(); }
        private void Update()
        {
            if (layout == null || movement == null || !movement.CanUseTraversal) return;
            var p = transform.position;
            var b = layout.playableBounds;
            if (p.y < layout.fallResetHeight || p.x < b.min.x || p.x > b.max.x || p.z < b.min.z || p.z > b.max.z)
            {
                movement.ReturnToSpawn();
                FindFirstObjectByType<ThirdPersonCameraController>()?.SnapToRoundStart(transform);
            }
        }
    }
}
