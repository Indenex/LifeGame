using Godot;

namespace LifeGame;

// A woven grass sleeping mat, lying along +Z from its foot, with a rolled-up head end.
public static partial class StructureModel
{
    static Mesh? mat;

    public static Mesh Mat => mat ??= MakeMat();

    static Mesh MakeMat()
    {
        var st = Begin();
        const float half = 0.36f, length = 1.75f, strip = 0.09f;
        // Strips of plaited grass across the mat, a little uneven, each its own shade.
        int strips = Mathf.RoundToInt(length / strip);
        for (int i = 0; i < strips; i++)
        {
            float z0 = -length / 2 + i * strip + 0.004f, z1 = z0 + strip - 0.008f;
            float lift = 0.012f + Hash(i, 61) * 0.01f;
            var shade = Shade(i * 9 + 3).Lerp(new Color(0.92f, 0.86f, 0.62f), 0.25f + 0.2f * (i % 2));
            float zc = (z0 + z1) / 2;
            OrientedBox(st, new Vector3(-half - Hash(i, 62) * 0.03f, 0, zc), new Vector3(half + Hash(i, 63) * 0.03f, 0, zc), Vector3.Back, (z1 - z0) / 2, 0.005f, lift, shade);
        }
        // The rolled head end, like a pillow.
        StickInto(st, new Vector3(-half + 0.02f, 0.07f, length / 2 - 0.1f), new Vector3(half - 0.02f, 0.07f, length / 2 - 0.1f), 0.075f, Shade(71).Lerp(new Color(0.9f, 0.82f, 0.58f), 0.3f));
        st.GenerateNormals();
        return st.Commit();
    }
}
