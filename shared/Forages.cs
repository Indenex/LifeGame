using System;
using System.Collections.Generic;

namespace LifeGame.Shared
{
    // Wild food that grows back where it was picked (D-206: one Def per kind). A forage
    // spot is a ForageBush row; its Kind picks the Def. Berry bushes are Kind 0, and
    // about one in Needs.HazelEvery of them is a hazel (by row id, survival-30).
    public sealed class ForageDef
    {
        public byte Kind;
        public Item Yield;
        public uint PerPick;
        public long RegrowMicros;
        // The seasons it bears in; null for all year.
        public Season[]? Seasons;
        // The Use prompt, and what the server says when there is nothing to take.
        public string Prompt = "";
        public string OutOfSeason = "";
        public string NotReady = "";
        // How far its edge reaches from its center, so reach is measured to what you touch.
        public float Radius;

        public bool InSeason(Season season) => Seasons == null || Array.IndexOf(Seasons, season) >= 0;
    }

    public static class Forages
    {
        // Stored kinds (ForageBush.Kind). Append only.
        public const byte BerryBushKind = 0, MushroomKind = 1;

        public static readonly ForageDef Berries = new()
        {
            Kind = BerryBushKind, Yield = Item.Berries, PerPick = 3, RegrowMicros = Needs.BushRegrowMicros,
            Prompt = "Pick berries", NotReady = "No ripe berries yet", Radius = Needs.BushRadius,
        };

        public static readonly ForageDef Hazel = new()
        {
            Kind = BerryBushKind, Yield = Item.Hazelnuts, PerPick = (uint)Needs.HazelnutsPerPick, RegrowMicros = Needs.BushRegrowMicros,
            Seasons = new[] { Season.Autumn },
            Prompt = "Gather hazelnuts", OutOfSeason = "The hazelnuts are not ripe until autumn", NotReady = "No ripe hazelnuts yet",
            Radius = Needs.BushRadius,
        };

        // Mushrooms (survival-35, a lane default until Justin answers): small clusters in
        // the shade near trees, out from spring to autumn, never in winter.
        public static readonly ForageDef Mushrooms = new()
        {
            Kind = MushroomKind, Yield = Item.Mushrooms, PerPick = 3, RegrowMicros = Needs.MushroomRegrowMicros,
            Seasons = new[] { Season.Spring, Season.Summer, Season.Autumn },
            Prompt = "Gather mushrooms", OutOfSeason = "No mushrooms come up in the cold", NotReady = "None left to gather yet",
            Radius = 0.25f,
        };

        static readonly ForageDef[] ByKind = { Berries, Mushrooms };

        // What a forage spot is: its stored kind, and for a bush whether it is a hazel.
        public static ForageDef Of(byte kind, ulong id) =>
            kind == BerryBushKind ? (Needs.IsHazelBush(id) ? Hazel : Berries)
            : kind < ByKind.Length ? ByKind[kind] : Berries;

        public static IReadOnlyList<ForageDef> All => new[] { Berries, Hazel, Mushrooms };
    }
}
