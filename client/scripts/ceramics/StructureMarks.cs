using System;
using System.Collections.Generic;

namespace LifeGame;

// The maker (and slip) a set-down jar or trough was made with (ceramics-15, the structure_mark table).
// StructureView asks for it by structure id and stamps the model; Changed fires when a row arrives or
// changes, so a model drawn before its row arrived restyles itself.
public static class StructureMarks
{
    static readonly Dictionary<ulong, uint> Marks = new();

    public static event Action<ulong>? Changed;

    // The mark number (shared/Marks.cs and Slip.cs), or 0 for none.
    public static uint Of(ulong structureId) => Marks.GetValueOrDefault(structureId);

    public static void Set(ulong structureId, uint mark)
    {
        Marks[structureId] = mark;
        Changed?.Invoke(structureId);
    }

    public static void Remove(ulong structureId)
    {
        if (Marks.Remove(structureId)) Changed?.Invoke(structureId);
    }
}
