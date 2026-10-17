// Groups (ui-38, Justin: "just build a shell right now"). Players form a small group from the
// right-click player menu: no leader, anyone leaves any time, a member away for a day drops off,
// and the group ends when one is left. Today a group gives a member list and a group chat line
// ("/g ..."); perks (shared map markers, needs rings) come later as entries in Perks.
// Keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public static class Groups
    {
        public const int MaxMembers = 10;
        // An invitation waits this long for an answer.
        public const long InviteMicros = 60L * 1_000_000;
        // A member offline this long drops out of the group.
        public const long DropAfterMicros = 24L * 60 * 60 * 1_000_000;
        // How close you stand to invite someone (the player menu opens within 8 m; this
        // leaves room for lag).
        public const float InviteReach = 10f;

        // What being in a group gives. Empty in the shell: a perk is an entry here plus the
        // code that reads it, never a change to the group tables.
        public sealed record GroupPerk(byte Id, string Name);
        public static readonly GroupPerk[] Perks = Array.Empty<GroupPerk>();

        // "/g hi" goes to everyone in your group, wherever they are.
        public static bool IsGroupLine(string text) =>
            text.StartsWith("/g ", StringComparison.OrdinalIgnoreCase) || text.Equals("/g", StringComparison.OrdinalIgnoreCase);
        public static string GroupBody(string text) => text.Length > 2 ? text[2..].Trim() : "";
    }
}
