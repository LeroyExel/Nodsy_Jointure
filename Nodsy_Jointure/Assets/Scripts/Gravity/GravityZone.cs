using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GravityZone : MonoBehaviour
{
    public enum GravityMode
    {
        ZoneUp,
        ZoneDown,
        ZoneForward,
        ZoneBack,
        ZoneRight,
        ZoneLeft,
        CustomDirection
    }

    [SerializeField] private GravityMode mode = GravityMode.ZoneLeft;
    [SerializeField] private Vector3 customDirection = Vector3.down;
    [SerializeField] private float gravityMagnitude = 9.81f;
    [SerializeField] private bool drawGizmos = true;

    private readonly HashSet<Rigidbody> trackedBodies = new HashSet<Rigidbody>();

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null)
        {
            trackedBodies.Add(other.attachedRigidbody);
            other.attachedRigidbody.useGravity = false;
        }
    }

    private void FixedUpdate()
    {
        trackedBodies.RemoveWhere(rb => rb == null);

        Vector3 force = GetGravityDirection() * gravityMagnitude;

        foreach (Rigidbody rb in trackedBodies)
        {
            if (!rb.isKinematic)
            {
                rb.AddForce(force, ForceMode.Acceleration);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null)
        {
            other.attachedRigidbody.useGravity = true;
            trackedBodies.Remove(other.attachedRigidbody);
        }
    }

    public Vector3 GetGravityDirection()
    {
        switch (mode)
        {
            case GravityMode.ZoneUp:
                return transform.up;
            case GravityMode.ZoneDown:
                return -transform.up;
            case GravityMode.ZoneForward:
                return transform.forward;
            case GravityMode.ZoneBack:
                return -transform.forward;
            case GravityMode.ZoneRight:
                return transform.right;
            case GravityMode.ZoneLeft:
                return -transform.right;
            case GravityMode.CustomDirection:
                return customDirection.sqrMagnitude > 0.001f ? customDirection.normalized : Vector3.down;
            default:
                return Vector3.down;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;
        Vector3 dir = GetGravityDirection();
        Gizmos.DrawRay(center, dir * 2f);
        Gizmos.DrawSphere(center + dir * 2f, 0.15f);
    }
}