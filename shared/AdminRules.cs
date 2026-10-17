namespace LifeGame.Shared
{
    // Limits the admin item panel and the server share (admin-1).
    public static class AdminRules
    {
        // Items one admin_give call may add.
        public const uint GiveMax = 999;
        // How far from the origin admin_teleport may put a player (a sanity bound, not a world edge).
        public const float TeleportMax = 20000f;
    }
}
