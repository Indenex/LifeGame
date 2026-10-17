using Godot;

namespace LifeGame;

// The painted look for the animals (painted-4): sheep, deer and hens on Painted's
// shader, so fleece, coats, faces and legs get the stepped light bands, violet shade
// and soft ink edge. Each source material (one per fleece or coat colour) gets its own
// painted twin, so the per-animal tints keep working.
public static class PaintedAnimals
{
    // Fleece and feathers: soft, not crisp. Barely any crevice tone, gentle broad dabs,
    // a lighter ink line so the wool edge reads as fluff, not outline.
    public static ShaderMaterial Wool(ShaderMaterial source)
    {
        var twin = Painted.Of(source);
        twin.SetShaderParameter("crevice", 0.08f);
        twin.SetShaderParameter("lit_dabs", 0.14f);
        twin.SetShaderParameter("dab_size", 0.7f);
        twin.SetShaderParameter("ink", 1.0f);
        twin.SetShaderParameter("shade_tint", new Color(0.7f, 0.66f, 0.88f));
        return twin;
    }

    // Short coats (deer): smooth hide, a firmer ink edge.
    public static ShaderMaterial Coat(ShaderMaterial source) => CoatTwin(source);

    static ShaderMaterial CoatTwin(ShaderMaterial source)
    {
        var twin = Painted.Of(source);
        twin.SetShaderParameter("crevice", 0.15f);
        twin.SetShaderParameter("lit_dabs", 0.12f);
        twin.SetShaderParameter("dab_size", 1.1f);
        twin.SetShaderParameter("ink", 1.3f);
        twin.SetShaderParameter("shade_tint", new Color(0.62f, 0.56f, 0.86f));
        return twin;
    }

    // Skin, faces, legs, beaks and antlers: vertex-coloured, on one white material.
    public static ShaderMaterial Skin => skin ??= CoatTwin(Look.Solid(Colors.White));
    static ShaderMaterial? skin;
}
