// claims-19: the offices a town's signers vote for, as data (D-206). Adding or changing an
// office is a row here, not new code; what an office can do is its Powers, checked on the server.
// Keep this file free of engine and SpacetimeDB types.
using System;
using System.Linq;

namespace LifeGame.Shared
{
    [Flags]
    public enum TownPower : ushort
    {
        None = 0,
        Remove = 1,      // take a claim off the roster
        Rename = 2,      // rename the town
        Announce = 4,    // send a word to every signer
        ClearRuins = 8,  // take down buildings left by those who have gone
        Merge = 16,      // propose joining another town
        BreakTies = 32,  // the holder's vote settles a tied election
        Ban = 64,        // keep a player off the town's claims for a set time
        Gates = 128,     // bar or unbar the gates on the town's claims
        Log = 256,       // add notes to the town's log
        Store = 512,     // take things out of the town's store (anyone on the roster puts in)
    }

    public sealed record TownOfficeDef(byte Id, string Name, string Job, TownPower Powers, byte StandsInFor = TownOfficeDef.NoOffice)
    {
        public const byte NoOffice = 255;
    }

    public static class Towns
    {
        // Ids are append-only (the Mayor is Social.OfficeMayor, 0).
        public static readonly TownOfficeDef[] Offices =
        {
            new(0, "Mayor", "Removes a claim from the roster, renames the town, proposes a merge, speaks to all, takes from the store.", TownPower.Remove | TownPower.Rename | TownPower.Merge | TownPower.Announce | TownPower.ClearRuins | TownPower.Store),
            new(1, "Deputy", "Stands in for the Mayor while the seat is empty.", TownPower.None, 0),
            new(2, "Judge", "Settles a tied election.", TownPower.BreakTies),
            new(3, "Herald", "Sends a word to the whole town.", TownPower.Announce),
            new(4, "Warden", "Clears ruins left by those who have gone.", TownPower.ClearRuins),
            new(5, "Marshal", "Keeps the peace: bans a player from the town's claims for a time.", TownPower.Ban),
            new(6, "Treasurer", "Keeps the town's store: takes things out of it for the town.", TownPower.Store),
            new(7, "Gatekeeper", "Bars and unbars the gates on the town's claims.", TownPower.Gates),
            new(8, "Scribe", "Keeps the town's log and adds notes to it.", TownPower.Log),
            new(9, "Elder", "Settles a tied election.", TownPower.BreakTies),
        };

        public static TownOfficeDef? Get(byte id) => Offices.FirstOrDefault(o => o.Id == id);

        // Rate limits and sizes, raisable (D-197).
        public const int AnnounceMax = 120;
        public const long AnnounceEveryMicros = 60L * 1_000_000;
        public const long MergeWindowMicros = 3L * 24 * 3600 * 1_000_000;
        public const float ClearRuinReach = 8f;

        // claims-21: a Marshal's ban lasts whole in-game days (WorldTime.DayLengthSeconds); raisable (D-197).
        public const int BanDaysDefault = 1;
        public const int BanDaysMax = 7;
        public const int LogKeep = 200;           // entries kept per town, oldest go first
        public const int LogNoteMax = 120;
        public const long LogNoteEveryMicros = 10L * 1_000_000;
        // claims-28: different stacks the town's store holds; raisable (D-197).
        public const int StoreStacksMax = 40;
    }
}
