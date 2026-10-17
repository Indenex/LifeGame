// How a player's body moves while a craft job runs (Animation lane, anim-2). A process
// names its motion on its ProcessDef; the client poses the figure from it. Append-only:
// never renumber or reorder.
namespace LifeGame.Shared
{
    public enum WorkMotion : byte
    {
        // Unset on the Def: the client falls back to its old guess from the process id.
        None,
        Gather, Knap, HandDrill, BowDrill, Twist, Knead, Scrape, Wrap, Carve, Tend,
        // Standing at a waist-high station (D-098).
        Station,
        // Two-handed axe swing at a trunk.
        Chop,
        // Quick actions, played once (anim-4, server PlayerAct): scoop water to the mouth
        // at a bank, food to the mouth, pluck from a bush, grab something off the ground.
        Drink, Eat, Pluck, PickUp,
        // More quick actions (anim-5): a strike (its style from the weapon: spear thrust,
        // blade slash, fist), drawing and loosing a bow, digging, shearing, butchering,
        // feeding a fire, and flinching from a blow.
        Strike, Shoot, Dig, Shear, Butcher, Feed, Flinch,
        // Emotes (anim-16, shared/Emotes.cs): played once on request, seen by everyone.
        Wave, Cheer, Dance, Bow,
        // More emotes (anim-19).
        Clap, Shrug, Point,
        // Craft loop (anim-24): crushing with a stone held in both hands.
        Pound,
    }
}
