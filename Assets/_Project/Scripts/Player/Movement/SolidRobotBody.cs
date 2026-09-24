using System.Collections.Generic;
using UnityEngine;

namespace Robot.Player.Movement
{
    // Animation never owns collision state. The body stays solid through attacks,
    // shutdown and recovery, including limbs outside the standing controller.
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class SolidRobotBody : MonoBehaviour
    {
        private static readonly List<SolidRobotBody> bodies = new List<SolidRobotBody>();
        private CharacterController controller;
        private Renderer[] visuals;
        private BoxCollider hull;
        private Bounds worldBounds;
        public Bounds BodyBounds => worldBounds;
        public BoxCollider Hull => hull;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => bodies.Clear();
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            visuals = GetComponentsInChildren<Renderer>(true);
            var proxy = new GameObject("SolidRobotHull");
            proxy.layer = LayerMask.NameToLayer("Ignore Raycast");
            // Only the robot root owns its gameplay tag. Tag-based player lookup
            // must never select this animation-sized, moving collision proxy.
            proxy.tag = "Untagged";
            proxy.transform.SetParent(transform, false);
            hull = proxy.AddComponent<BoxCollider>();
            // Only self-collision is excluded. No layer matrix or robot pair is changed.
            foreach (var own in GetComponentsInChildren<Collider>(true))
                if (own != hull) Physics.IgnoreCollision(own, hull, true);
            RefreshHull();
        }
        private void OnEnable() { if (!bodies.Contains(this)) bodies.Add(this); }
        private void OnDisable() { bodies.Remove(this); }
        private void LateUpdate() { }
        public void RefreshHull()
        {
            if (hull == null || controller == null) return;
            
            Bounds bounds = default;
            bool hasBounds = false;

            // Collect exact 3D world positions of character bones and renderers
            foreach (var visual in visuals)
            {
                if (visual == null || !visual.enabled || !visual.gameObject.activeInHierarchy) continue;

                if (visual is SkinnedMeshRenderer skinned)
                {
                    var bones = skinned.bones;
                    if (bones != null && bones.Length > 0)
                    {
                        foreach (var bone in bones)
                        {
                            if (bone == null) continue;
                            Vector3 pos = bone.position;
                            if (!hasBounds)
                            {
                                bounds = new Bounds(pos, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                bounds.Encapsulate(pos);
                            }
                        }
                    }
                    else
                    {
                        if (!hasBounds) { bounds = skinned.bounds; hasBounds = true; }
                        else bounds.Encapsulate(skinned.bounds);
                    }
                }
                else if (visual is MeshRenderer mesh)
                {
                    if (!hasBounds) { bounds = mesh.bounds; hasBounds = true; }
                    else bounds.Encapsulate(mesh.bounds);
                }
            }

            if (!hasBounds)
            {
                bounds = new Bounds(transform.TransformPoint(controller.center),
                    new Vector3(controller.radius * 2, controller.height, controller.radius * 2));
            }
            else
            {
                // Add character body radius padding around bone skeleton
                bounds.Expand(new Vector3(controller.radius * 2f, 0.1f, controller.radius * 2f));
            }

            // Ensure Y size is at least standing height, anchored at ground (bounds.min.y)
            float minHeight = Mathf.Max(controller.height, 1.8f);
            float minY = bounds.min.y;
            if (bounds.size.y < minHeight)
            {
                bounds.size = new Vector3(bounds.size.x, minHeight, bounds.size.z);
                bounds.center = new Vector3(bounds.center.x, minY + minHeight * 0.5f, bounds.center.z);
            }

            bounds.Expand(0.04f);
            worldBounds = bounds;
            hull.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);
            float scaleX = Mathf.Abs(transform.lossyScale.x) > 0.001f ? Mathf.Abs(transform.lossyScale.x) : 1f;
            float scaleY = Mathf.Abs(transform.lossyScale.y) > 0.001f ? Mathf.Abs(transform.lossyScale.y) : 1f;
            float scaleZ = Mathf.Abs(transform.lossyScale.z) > 0.001f ? Mathf.Abs(transform.lossyScale.z) : 1f;
            hull.transform.localScale = new Vector3(1f / scaleX, 1f / scaleY, 1f / scaleZ);
            hull.center = Vector3.zero;
            hull.size = bounds.size;
            hull.enabled = true;
            hull.isTrigger = true;
            if (!Physics.GetIgnoreCollision(controller, hull)) Physics.IgnoreCollision(controller, hull, true);
        }
        public static CollisionFlags Move(CharacterController mover, Vector3 displacement)
        {
            Vector3 horizontal = new Vector3(displacement.x, 0, displacement.z);
            float length = horizontal.magnitude;
            if (length > .00001f)
            {
                Vector3 origin = mover.transform.TransformPoint(mover.center);
                float radius = mover.radius * Mathf.Max(Mathf.Abs(mover.transform.lossyScale.x), Mathf.Abs(mover.transform.lossyScale.z));
                float allowed = length;
                foreach (var body in bodies)
                {
                    if (body == null || body.controller == mover || body.hull == null) continue;
                    body.RefreshHull();
                    Bounds bounds = body.BodyBounds;
                    // Prevent mounting a robot, including during a jump over a fallen body.
                    float feet = origin.y - mover.height * .5f;
                    if (feet > bounds.max.y || origin.y + mover.height * .5f < bounds.min.y) continue;
                    bounds.Expand(new Vector3(radius * 2 + .04f, 0, radius * 2 + .04f));
                    var projected = new Vector3(origin.x, bounds.center.y, origin.z);
                    if (bounds.Contains(projected))
                    {
                        // Allow escape from an externally introduced overlap, never move deeper or stay inside.
                        Vector3 away = projected - bounds.center;
                        away.y = 0;
                        if (away.sqrMagnitude < 0.0001f) away = mover.transform.forward;
                        if (Vector3.Dot(horizontal, away) <= 0) allowed = 0;
                    }
                    else if (bounds.IntersectRay(new Ray(projected, horizontal.normalized), out float distance))
                        allowed = Mathf.Min(allowed, Mathf.Max(0, distance - .015f));
                }
                horizontal *= allowed / length;
            }
            return mover.Move(horizontal + Vector3.up * displacement.y);
        }
    }
}
