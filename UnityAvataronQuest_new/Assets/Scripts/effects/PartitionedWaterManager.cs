// PartitionedWaterManager.cs
// Room is a world-space axis-aligned rectangle on XZ.
// Each player "owns" the Voronoi cell clipped to the room.
// Water is rendered per-player when player.waterOn == true.
// Sync only: player transforms (usually already) + waterOn booleans (and stable ordering / ids).

using System;
using System.Collections.Generic;
using UnityEngine;

public class PartitionedWaterManager : MonoBehaviour
{
    [Header("Room Source")]
    private Vector3 roomCenter = Vector3.zero;
    private Vector2 roomSizeXZ = new Vector2(6f, 6f); // width (X), depth (Z)
    [SerializeField] private Collider floorCollider;

    [Header("Water")]
    public float waterY = 0.1f; // ankle-height
    public Material waterMaterial;

    [Header("Players (seed points)")]
    public List<PlayerRegion> players = new();

    [Header("Performance")]
    [Tooltip("If > 0, recompute regions at this frequency (Hz). 0 = every frame.")]
    public float recomputeHz = 10f;

    float _nextRecomputeTime;

    [Serializable]
    public class PlayerRegion
    {
        public string id;
        public Transform player;
        public bool waterOn;

        [HideInInspector] public Mesh mesh;
        [HideInInspector] public MeshFilter mf;
        [HideInInspector] public MeshRenderer mr;
    }

    void Awake()
    {
        CacheRoomFromFloor();

        // Create a child water object per player
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null) continue;
            InitRegionObject(players[i], i);
        }
    }

    void OnValidate()
    {
        // Keep recomputeHz sane
        if (recomputeHz < 0f) recomputeHz = 0f;
    }

    void LateUpdate()
    {
        if (players == null || players.Count == 0) return;

        if (recomputeHz > 0f)
        {
            if (Time.time < _nextRecomputeTime) return;
            _nextRecomputeTime = Time.time + (1f / recomputeHz);
        }

        // Toggle renderers; if nothing is on, just clear & return
        bool anyOn = false;
        for (int i = 0; i < players.Count; i++)
        {
            var pr = players[i];
            if (pr == null || pr.mr == null) continue;
            bool on = pr.waterOn && pr.player != null;
            pr.mr.enabled = on;
            anyOn |= on;
            if (!on && pr.mesh != null) pr.mesh.Clear();
        }
        if (!anyOn) return;

        // Room polygon (clockwise) in XZ
        List<Vector2> roomPoly = BuildRoomRectPolygonXZ(roomCenter, roomSizeXZ);

        // Pre-sample all player XZ positions (stable within this tick)
        Vector2[] seeds = new Vector2[players.Count];
        bool[] valid = new bool[players.Count];

        for (int i = 0; i < players.Count; i++)
        {
            var pr = players[i];
            if (pr != null && pr.player != null)
            {
                seeds[i] = ToXZ(pr.player.position);
                valid[i] = true;
            }
        }

        // For each player i: poly = room clipped by all half-planes (closer to i than j)
        for (int i = 0; i < players.Count; i++)
        {
            var me = players[i];
            if (me == null || me.mesh == null) continue;

            if (!me.waterOn || !valid[i])
            {
                me.mesh.Clear();
                continue;
            }

            // Start from room
            var poly = new List<Vector2>(roomPoly);

            Vector2 Pi = seeds[i];

            for (int j = 0; j < players.Count; j++)
            {
                if (j == i || !valid[j]) continue;

                Vector2 Pj = seeds[j];
                Vector2 d = Pj - Pi;

                // If too close, skip to avoid numerical issues
                if (d.sqrMagnitude < 1e-8f) continue;

                Vector2 mid = 0.5f * (Pi + Pj);
                Vector2 n = d; // normal toward "other" side

                // Keep side closer to Pi: (x - mid)¡¤(Pj - Pi) <= 0
                poly = ClipPolygonByHalfPlane(poly, mid, n);

                if (poly.Count < 3) break;
            }

            if (poly.Count >= 3)
                BuildMeshFromConvexPolygonXZ(poly, waterY, me.mesh);
            else
                me.mesh.Clear();
        }
    }

    // Call this from your networking layer after sync (or locally for demo)
    public void SetWaterOn(string playerId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].id == playerId)
            {
                players[i].waterOn = true;
                return;
            }
        }
    }

    public void SetWaterOff(string playerId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].id == playerId)
            {
                players[i].waterOn = false;
                return;
            }
        }
    }

    // --- Init per player water object ---
    void InitRegionObject(PlayerRegion p, int index)
    {
        if (string.IsNullOrEmpty(p.id))
            p.id = $"P{index}";

        var go = new GameObject($"Water_{p.id}");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        p.mf = go.AddComponent<MeshFilter>();
        p.mr = go.AddComponent<MeshRenderer>();
        if (waterMaterial != null) p.mr.sharedMaterial = waterMaterial;

        p.mesh = new Mesh { name = $"WaterMesh_{p.id}" };
        p.mf.sharedMesh = p.mesh;

        p.mr.enabled = false;
    }

    // --- Geometry helpers ---

    static Vector2 ToXZ(Vector3 p) => new Vector2(p.x, p.z);

    static List<Vector2> BuildRoomRectPolygonXZ(Vector3 center, Vector2 sizeXZ)
    {
        float hx = sizeXZ.x * 0.5f;
        float hz = sizeXZ.y * 0.5f;

        // Clockwise order
        return new List<Vector2>
        {
            new Vector2(center.x - hx, center.z - hz),
            new Vector2(center.x + hx, center.z - hz),
            new Vector2(center.x + hx, center.z + hz),
            new Vector2(center.x - hx, center.z + hz),
        };
    }

    // Clip polygon by half-plane: keep points satisfying (x - p0)¡¤n <= 0
    static List<Vector2> ClipPolygonByHalfPlane(List<Vector2> poly, Vector2 p0, Vector2 n)
    {
        var outPoly = new List<Vector2>(poly.Count + 2);
        if (poly == null || poly.Count == 0) return outPoly;

        bool Inside(Vector2 x) => Vector2.Dot(x - p0, n) <= 0f;

        Vector2 S = poly[poly.Count - 1];
        bool S_in = Inside(S);

        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 E = poly[i];
            bool E_in = Inside(E);

            if (E_in)
            {
                if (!S_in) outPoly.Add(IntersectSegmentWithLine(S, E, p0, n));
                outPoly.Add(E);
            }
            else if (S_in)
            {
                outPoly.Add(IntersectSegmentWithLine(S, E, p0, n));
            }

            S = E;
            S_in = E_in;
        }

        // remove near-duplicates
        const float eps = 1e-5f;
        for (int i = outPoly.Count - 1; i > 0; i--)
        {
            if ((outPoly[i] - outPoly[i - 1]).sqrMagnitude < eps * eps)
                outPoly.RemoveAt(i);
        }
        if (outPoly.Count >= 2 && (outPoly[0] - outPoly[outPoly.Count - 1]).sqrMagnitude < eps * eps)
            outPoly.RemoveAt(outPoly.Count - 1);

        return outPoly;
    }

    // Intersection of segment [A,B] with line (x - p0)¡¤n = 0 in 2D
    static Vector2 IntersectSegmentWithLine(Vector2 A, Vector2 B, Vector2 p0, Vector2 n)
    {
        Vector2 AB = B - A;
        float denom = Vector2.Dot(AB, n);
        if (Mathf.Abs(denom) < 1e-8f) return A; // almost parallel

        float t = Vector2.Dot(p0 - A, n) / denom; // dot(A + tAB - p0, n) = 0
        t = Mathf.Clamp01(t);
        return A + t * AB;
    }

    // For our case, polygon remains convex (rect clipped by half-planes), so fan triangulation works.
    static void BuildMeshFromConvexPolygonXZ(List<Vector2> poly, float y, Mesh outMesh)
    {
        int n = poly.Count;
        if (n < 3)
        {
            outMesh.Clear();
            return;
        }

        var verts = new Vector3[n];
        var uvs = new Vector2[n];

        // Simple world-space UV scaling (adjust to taste)
        const float uvScale = 0.1f;

        for (int i = 0; i < n; i++)
        {
            verts[i] = new Vector3(poly[i].x, y, poly[i].y);
            uvs[i] = poly[i] * uvScale;
        }

        var tris = new int[(n - 2) * 3];
        int t = 0;
        for (int i = 1; i < n - 1; i++)
        {
            tris[t++] = 0;
            tris[t++] = i + 1;
            tris[t++] = i;
        }

        outMesh.Clear();
        outMesh.vertices = verts;
        outMesh.uv = uvs;
        outMesh.triangles = tris;
        outMesh.RecalculateNormals();
        outMesh.RecalculateBounds();
    }

    void CacheRoomFromFloor()
    {
        if (floorCollider == null)
        {
            Debug.LogError("Floor Collider not assigned.");
            return;
        }

        Bounds b = floorCollider.bounds;

        roomCenter = new Vector3(b.center.x, 0f, b.center.z);
        roomSizeXZ = new Vector2(b.size.x, b.size.z);
    }
}
