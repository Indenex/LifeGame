using Godot;

namespace LifeGame;

// The painted ground (env-25, Justin: "the painted look on everything"). The terrain is
// drawn with Modeling's painted twin of Look.Ground (Painted.Of), so the ground speaks
// the same language as the painted trees and props: stepped light with violet shade,
// broad brush dabs instead of the fine brushed noise, crevices in one darker tone.
// Ground needs no ink edge (it would draw dark lines along every slope) and bigger dabs,
// read as broad hand-painted patches across the meadow.
public static class PaintedTerrain
{
    public static ShaderMaterial Ground => ground ??= Make();
    static ShaderMaterial? ground;

    static ShaderMaterial Make()
    {
        var material = (ShaderMaterial)Painted.Of(Look.Ground).Duplicate();
        material.SetShaderParameter("ink", 0f);
        material.SetShaderParameter("dab_size", 4.5f);
        material.SetShaderParameter("lit_dabs", 0.12f);
        material.SetShaderParameter("shade_tint", new Color(0.72f, 0.7f, 0.88f));
        material.SetShaderParameter("crevice", 0.3f);
        // The fine brushed noise was the flat look Justin disliked; a lighter touch of it.
        material.SetShaderParameter("brush_strength", 0.07f);
        // Facing the sun, the ground would take the props' stepped light too bright;
        // this keeps it at the old ground's level, the same scale the grass uses.
        float level = Mathf.Pow(Look.PaintedGroundLevel, 1f / 2.2f);
        material.SetShaderParameter("albedo", new Color(level, level, level));
        return material;
    }
}
