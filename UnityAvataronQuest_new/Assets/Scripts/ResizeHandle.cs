using UnityEngine;

public class ResizeHandle : MonoBehaviour
{
    public int cornerIndex;

    public Vector3 GetSignVector()
    {
        int xBit = (cornerIndex & 0b100) >> 2;
        int yBit = (cornerIndex & 0b010) >> 1;
        int zBit = (cornerIndex & 0b001);
        float sx = xBit == 1 ? 1f : -1f;
        float sy = yBit == 1 ? 1f : -1f;
        float sz = zBit == 1 ? 1f : -1f;
        return new Vector3(sx, sy, sz);
    }

    public int GetOppositeCornerIndex()
    {
        return cornerIndex ^ 0b111; 
    }
}
