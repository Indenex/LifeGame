using Godot;

namespace LifeGame;

// The fire pit (model-1 follow-up, for Crafting's FireView): a ring of fieldstones,
// each its own shape and size, in one mesh; and what is left when the fire dies,
// a soft bed of grey ash with charred stubs of wood and bits of charcoal.
public static class HearthModel
{
    static ArrayMesh? ring, coldAsh;

    static readonly Color Ash = new(0.38f, 0.37f, 0.35f);
    static readonly Color AshPale = new(0.55f, 0.54f, 0.51f);
    static readonly Color Char = new(0.10f, 0.09f, 0.08f);
    static readonly Color Scorch = new(0.20f, 0.17f, 0.14f);

    static float H(int a, int b) => MeshKit.Hash(a, b + 40);

    static Material Relief(float scale, float bump, float cavity)
    {
        var material = Look.Solid(Colors.White);
        material.SetShaderParameter("detail_scale", scale);
        material.SetShaderParameter("bump", bump);
        material.SetShaderParameter("cavity", cavity);
        return material;
    }

    // Nine stones round a 0.45 m circle, sooty on the inside faces.
    public static ArrayMesh Ring
    {
        get
        {
            if (ring != null) return ring;
            var k = new MeshKit(true) { Skinned = false };
            const int stones = 9;
            for (int i = 0; i < stones; i++)
            {
                float a = (i + (H(i, 1) - 0.5f) * 0.3f) * Mathf.Tau / stones;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float size = 0.85f + H(i, 2) * 0.35f;
                var radii = new Vector3(0.11f, 0.075f, 0.085f) * size;
                var at = dir * (0.45f + (H(i, 3) - 0.5f) * 0.04f) + Vector3.Up * radii.Y * 0.6f;
                var turn = new Basis(Vector3.Up, -a + (H(i, 4) - 0.5f) * 0.5f);
                k.Paint(Look.Rock, 0.68f + H(i, 5) * 0.2f).Ellipsoid(at, radii, 6, 4, turn, 0.5f);
                // Soot on the side that faced the fire.
                k.Paint(Scorch, 1f).Ellipsoid(at - dir * radii.Z * 0.42f, radii * new Vector3(0.7f, 0.8f, 0.6f), 6, 3, turn, 0.5f);
            }
            ring = new ArrayMesh();
            k.Commit(ring, PaintedStructures.Of(Relief(5f, 0.6f, 0.25f), PaintedStructures.Family.Stone));
            return ring;
        }
    }

    // The ash bed and burnt-out stubs, inside the ring.
    public static ArrayMesh ColdAsh
    {
        get
        {
            if (coldAsh != null) return coldAsh;
            var ash = new MeshKit(true) { Skinned = false };
            ash.Paint(Ash).Ellipsoid(new Vector3(0, 0f, 0), new Vector3(0.34f, 0.045f, 0.32f), 10, 3, null, 0.2f);
            ash.Paint(AshPale, 0.95f).Ellipsoid(new Vector3(0.03f, 0.02f, -0.02f), new Vector3(0.2f, 0.035f, 0.18f), 8, 3, null, 0.2f);
            // Bits of charcoal scattered in the ash.
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.4f;
                float r = 0.06f + H(i, 6) * 0.2f;
                ash.Paint(Char, 1f + H(i, 7) * 0.4f).Ellipsoid(new Vector3(Mathf.Cos(a) * r, 0.03f, Mathf.Sin(a) * r), new Vector3(0.025f, 0.015f, 0.018f) * (0.7f + H(i, 8)), 5, 3, new Basis(Vector3.Up, a));
            }

            var wood = new MeshKit(true) { Skinned = false };
            // Three stubs that burnt from the middle out: charred ends, a little
            // bark left where they lay on the stones.
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.Tau / 3f + 0.4f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var outer = dir * 0.42f + Vector3.Up * 0.07f;
                var inner = dir * (0.1f + H(i, 9) * 0.05f) + Vector3.Up * 0.035f;
                wood.Paint(new Color(0.27f, 0.22f, 0.17f)).Tube(new[] { outer, outer.Lerp(inner, 0.45f) }, 0.038f, 6, 0.035f);
                wood.Paint(Char).Tube(new[] { outer.Lerp(inner, 0.43f), inner }, 0.035f, 6, 0.012f);
                wood.Paint(AshPale, 0.9f).Ellipsoid(inner + Vector3.Up * 0.008f, new Vector3(0.02f, 0.012f, 0.02f), 5, 3);
            }

            coldAsh = new ArrayMesh();
            ash.Commit(coldAsh, PaintedStructures.Of(Relief(9f, 0.35f, 0.2f), PaintedStructures.Family.Clay));
            wood.Commit(coldAsh, PaintedStructures.Of(Relief(7f, 0.9f, 0.35f), PaintedStructures.Family.Wood));
            return coldAsh;
        }
    }
}
