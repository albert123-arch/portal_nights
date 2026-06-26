using UnityEngine;

namespace PortalNights
{
    public sealed class PortalNightsLanePath : MonoBehaviour
    {
        [SerializeField] private PortalNightsLane lane;
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private Color gizmoColor = new Color(0.15f, 0.75f, 1f, 0.9f);
        [SerializeField] private float waypointRadius = 0.36f;

        public PortalNightsLane Lane => lane;
        public int WaypointCount => waypoints == null ? 0 : waypoints.Length;
        public float PathLength => CalculateLength();

        public void Configure(PortalNightsLane newLane, Transform[] newWaypoints, Color color)
        {
            lane = newLane;
            waypoints = newWaypoints;
            gizmoColor = color;
        }

        public bool TryGetWaypoint(int index, out Vector3 position)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length || waypoints[index] == null)
            {
                position = transform.position;
                return false;
            }

            position = waypoints[index].position;
            return true;
        }

        private float CalculateLength()
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                return 0f;
            }

            float length = 0f;
            Vector3 previous = waypoints[0] == null ? transform.position : waypoints[0].position;
            for (int i = 1; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                Vector3 current = waypoints[i].position;
                length += Vector3.Distance(PortalNightsMath.Flat(previous), PortalNightsMath.Flat(current));
                previous = current;
            }

            return length;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                return;
            }

            Gizmos.color = gizmoColor;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }

                Vector3 position = waypoints[i].position + Vector3.up * 0.08f;
                Gizmos.DrawWireSphere(position, waypointRadius);
                if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(position, waypoints[i + 1].position + Vector3.up * 0.08f);
                }

#if UNITY_EDITOR
                UnityEditor.Handles.color = gizmoColor;
                UnityEditor.Handles.Label(position + Vector3.up * 0.35f, $"{lane} {i + 1}");
#endif
            }
        }
    }
}
