using LifeGame.Shared;
using SpacetimeDB;

// Who made the jar or trough a structure was set down from (ceramics-15, crafting-25), and its slip.
// The same number a pot carries (ClayPiece.Mark): the maker bits from shared/Marks.cs. Written by the
// stage that took the item (Building.Build asks RememberStructureMark); the client stamps the set-down
// model with it. Removed with the structure (CeramicsStructureRemoved).
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "StructureMark", Public = true)]
    public partial struct StructureMark
    {
        [PrimaryKey]
        public ulong StructureId;
        public uint Mark;
    }

    // Notes the mark of the pot, jar or trough a structure stage is about to take. Nothing is written
    // for an item with no maker.
    static void RememberStructureMark(ReducerContext ctx, ulong structureId, uint mark)
    {
        if (mark == 0) return;
        var row = new StructureMark { StructureId = structureId, Mark = mark };
        if (ctx.Db.StructureMark.StructureId.Find(structureId) is null) ctx.Db.StructureMark.Insert(row);
        else ctx.Db.StructureMark.StructureId.Update(row);
    }
}
