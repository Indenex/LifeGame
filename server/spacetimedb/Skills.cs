using LifeGame.Shared;
using SpacetimeDB;

// What a skill does when it could still grow. At the total cap, a skill set to
// Fade gives up points so the skills set to Raise can keep growing.
[SpacetimeDB.Type]
public enum SkillMode
{
    Raise,
    Hold,
    Fade,
}

// Capped skills: 0 to 100 per specialization and 300 in total, with survival
// basics outside the cap. Skill makes work faster and raises the quality you
// can reach. Death never costs skill.
// Crafting reaches this through the partial hooks declared in Crafting.cs.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "SkillLevel")]
    [SpacetimeDB.Index.BTree(Accessor = "ByOwnerSpec", Columns = new[] { nameof(Owner), nameof(Spec) })]
    public partial struct SkillLevel
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity Owner;
        public string Spec;
        // Hundredths of a rank: 10000 is rank 100.
        public ushort Points;
        public SkillMode Mode;
    }

    [SpacetimeDB.View(Accessor = "MySkills", Public = true)]
    public static List<SkillLevel> MySkills(ViewContext ctx) =>
        ctx.Db.SkillLevel.Owner.Filter(ctx.Sender).ToList();

    // Crafting asks for the best quality this player can make with a process.
    static partial void QualityCapFor(ReducerContext ctx, uint playerId, string processId, ref byte cap) =>
        cap = Math.Min(cap, Skills.QualityCap(SkillRank(ctx, playerId, processId)));

    // Crafting asks how long a job takes this player: skill shortens it down to 40%.
    static partial void DurationFor(ReducerContext ctx, uint playerId, string processId, ref float seconds) =>
        seconds *= DurationScale(ctx, playerId, processId);

    // Rank 0 to 100 in the skill a process trains. 0 for processes no skill covers.
    static int SkillRank(ReducerContext ctx, uint playerId, string processId) =>
        Skills.SpecFor(processId) is { } spec
        && ctx.Db.Player.PlayerId.Find(playerId) is { } player
        && FindSkill(ctx, player.Identity, spec.Id) is { } row
            ? Skills.Rank(row.Points)
            : 0;

    // Multiplier on how long a process takes this player: 1 for a novice, 0.4 for a master.
    static float DurationScale(ReducerContext ctx, uint playerId, string processId) =>
        Skills.DurationScale(SkillRank(ctx, playerId, processId));

    // depth is the cell's depth before this dig: deep clay (ClayRules, D-041) or the
    // digger's skill, whichever grades it higher.
    static void GiveDugClay(ReducerContext ctx, Player player, int depth)
    {
        int rank = SkillRank(ctx, player.PlayerId, Skills.Dig);
        uint count = ctx.Rng.NextDouble() < Skills.SecondLumpChance(rank) ? 2u : 1u;
        byte quality = Math.Max(ClayRules.DugClayQuality(depth), Skills.DugClayQuality(rank));
        AddToInventory(ctx, player.Identity, Item.Clay, count, quality);
    }

    [SpacetimeDB.Reducer]
    public static void SetSkillMode(ReducerContext ctx, string specId, SkillMode mode)
    {
        RequirePlayer(ctx);
        var spec = Skills.Spec(specId) ?? throw new Exception("No such skill");
        if (spec.Basic)
            throw new Exception($"{spec.Name} is a survival basic and always grows");
        if (FindSkill(ctx, ctx.Sender, specId) is { } row)
            ctx.Db.SkillLevel.Id.Update(row with { Mode = mode });
        else
            ctx.Db.SkillLevel.Insert(new SkillLevel { Id = 0, Owner = ctx.Sender, Spec = specId, Points = 0, Mode = mode });
    }

    static SkillLevel? FindSkill(ReducerContext ctx, Identity who, string specId)
    {
        foreach (var row in ctx.Db.SkillLevel.ByOwnerSpec.Filter((who, specId)))
            return row;
        return null;
    }

    static void TrainSkill(ReducerContext ctx, Identity who, string processId, bool failed)
    {
        if (Skills.SpecFor(processId) is not { } spec) return;
        var row = FindSkill(ctx, who, spec.Id)
            ?? ctx.Db.SkillLevel.Insert(new SkillLevel { Id = 0, Owner = who, Spec = spec.Id, Points = 0, Mode = SkillMode.Raise });
        if (row.Mode != SkillMode.Raise) return;

        int gain = Math.Min(Skills.Gain(row.Points, failed, spec.Id), Skills.MaxPoints - row.Points);
        if (gain <= 0) return;
        if (!spec.Basic) gain = MakeRoom(ctx, who, spec.Id, gain);
        if (gain <= 0) return;

        ctx.Db.SkillLevel.Id.Update(row with { Points = (ushort)(row.Points + gain) });
    }

    // Under the total cap a skill grows freely. At the cap it can only grow by
    // taking points from skills set to Fade, biggest first. Returns the gain allowed.
    static int MakeRoom(ReducerContext ctx, Identity who, string growing, int gain)
    {
        var capped = ctx.Db.SkillLevel.Owner.Filter(who)
            .Where(s => Skills.Spec(s.Spec) is { Basic: false })
            .ToList();
        int room = Skills.CapPoints - capped.Sum(s => s.Points);
        if (room >= gain) return gain;

        int needed = gain - Math.Max(room, 0);
        foreach (var fading in capped.Where(s => s.Mode == SkillMode.Fade && s.Spec != growing && s.Points > 0)
                     .OrderByDescending(s => s.Points))
        {
            int take = Math.Min(needed, fading.Points);
            ctx.Db.SkillLevel.Id.Update(fading with { Points = (ushort)(fading.Points - take) });
            needed -= take;
            if (needed == 0) break;
        }
        return gain - needed;
    }
}
