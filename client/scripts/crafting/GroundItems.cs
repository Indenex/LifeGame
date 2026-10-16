using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Items lying on the ground, drawn as one MultiMesh per item type per patch of
// ground instead of one MeshInstance3D each. A fresh world scatters hundreds of
// sticks and stones, and one draw call apiece was a large share of the frame.
// Patches keep each batch's bounds small so off-screen ones are still culled.
public partial class GroundItems : Node3D
{
    const float PatchMeters = 32f;

    sealed class Batch
    {
        public required MultiMesh Mesh;
        public required Transform3D Hidden;
        public readonly List<ulong> Ids = new();
        public readonly List<Transform3D> Places = new();
    }

    readonly Dictionary<(Item, int, int), Batch> batches = new();
    readonly Dictionary<ulong, Batch> batchOf = new();
    readonly Dictionary<ulong, int> slotOf = new();

    public bool Has(ulong id) => batchOf.ContainsKey(id);

    public void Add(ulong id, Item item, Transform3D place)
    {
        if (batchOf.ContainsKey(id)) return;
        int px = Mathf.FloorToInt(place.Origin.X / PatchMeters), pz = Mathf.FloorToInt(place.Origin.Z / PatchMeters);
        if (!batches.TryGetValue((item, px, pz), out var batch))
        {
            batch = new Batch
            {
                Mesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = ItemLook.MeshFor(item) },
                // Unused slots collapse to a point inside the patch, so they draw
                // nothing and don't stretch the batch's bounds.
                Hidden = new Transform3D(new Basis(Vector3.Zero, Vector3.Zero, Vector3.Zero), place.Origin),
            };
            batches[(item, px, pz)] = batch;
            AddChild(new MultiMeshInstance3D { Multimesh = batch.Mesh });
        }

        int slot = batch.Ids.Count;
        batch.Ids.Add(id);
        batch.Places.Add(place);
        batchOf[id] = batch;
        slotOf[id] = slot;

        var mesh = batch.Mesh;
        if (slot >= mesh.InstanceCount)
        {
            // Resizing clears the buffer, so write every slot again.
            mesh.VisibleInstanceCount = -1;
            mesh.InstanceCount = Mathf.Max(8, mesh.InstanceCount * 2);
            for (int i = 0; i < mesh.InstanceCount; i++)
                mesh.SetInstanceTransform(i, i < batch.Places.Count ? batch.Places[i] : batch.Hidden);
        }
        else mesh.SetInstanceTransform(slot, place);
        mesh.VisibleInstanceCount = batch.Ids.Count;
    }

    public void Remove(ulong id)
    {
        if (!batchOf.Remove(id, out var batch) || !slotOf.Remove(id, out int slot)) return;
        // Move the last item into the freed slot so the live ones stay packed.
        int last = batch.Ids.Count - 1;
        if (slot != last)
        {
            ulong moved = batch.Ids[last];
            batch.Ids[slot] = moved;
            batch.Places[slot] = batch.Places[last];
            slotOf[moved] = slot;
            batch.Mesh.SetInstanceTransform(slot, batch.Places[slot]);
        }
        batch.Ids.RemoveAt(last);
        batch.Places.RemoveAt(last);
        batch.Mesh.SetInstanceTransform(last, batch.Hidden);
        batch.Mesh.VisibleInstanceCount = batch.Ids.Count;
    }
}
