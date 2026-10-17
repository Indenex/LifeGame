// The animal registry (Animals lane, docs/CONTENT.md): one AnimalDef per species holds
// how it moves and senses, what it drops, which behaviours it has and which model and
// voice it uses. Adding a species is a new Def here plus assets; the server's one animal
// behaviour and the client's one animal view read these. Species ids match the server's
// AnimalSpecies enum (append-only). Keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    // Where a species' first herds start and how big they are: the herd settles in the
    // most fitting ground within Search meters of the spot.
    public readonly record struct AnimalHerdSpot(float X, float Z, int Size, float Search);

    public sealed class AnimalDef
    {
        public byte Id;
        public string Key = "";
        public string Name = "";

        // Speeds in m/s.
        public float Walk, Trot, Run;
        // Players beyond this are not noticed at all.
        public float Notice;
        // Closing in slower than this never alarms.
        public float CalmSpeed;
        // How far a herd strays from home, and how far ahead a bolting animal aims.
        public float HomeRange, FleeAhead;
        // How far a leader wanders at a time.
        public float WanderMin, WanderMax;
        // The woodland it likes best, 0 open grass to 1 deep woods.
        public float WoodPref;
        // Will walk up to someone who stands still for a while.
        public bool Curious;
        // Inside this, anyone moving at all unsettles it, however slowly; 0 for none.
        public float Wary;

        // A wounded animal's running speed is (RunFloor + RunHealthShare * health / 100).
        public float RunFloor = 0.6f, RunHealthShare = 0.4f;
        // Below this health it stops to rest as soon as nothing close is pressing it; 0 for never.
        public int RestBelowHealth;

        // Capability flags: what you can do with it, and what it does.
        public bool Shearable;
        // Whether its flock grows lambs in spring.
        public bool Breeds = true;
        // Bleats like a sheep (Atmosphere's flock sound); deer have their own calls (Audio).
        public bool Bleats;

        // What butchering gives: meat (grown / young), a hide and an extra for grown animals.
        public Item Meat;
        public int MeatGrown = 3, MeatYoung = 1;
        public Item? Hide;
        public Item? Extra;

        // Laying (animals-19): the egg a grown animal lays, game days between eggs, and how many it holds
        // until collected (D-148: no chore, they just wait, up to the cap).
        public Item? Egg;
        public float EggDays;
        public byte EggCap;
        // Settles for the night in a coop (or a sheltered spot) and comes out at dawn.
        public bool Roosts;

        // The client's model plan ("sheep", "deer" or "hen") and the share of males that carry antlers.
        public string Model = "sheep";
        public float AntlerShare;

        public AnimalHerdSpot[] Herds = Array.Empty<AnimalHerdSpot>();
    }

    public static class AnimalDefs
    {
        public const byte Sheep = 0, Deer = 1, Hen = 2;

        // Sheep keep exactly the numbers they always had.
        public static readonly AnimalDef[] All =
        {
            new()
            {
                Id = Sheep, Key = "animal.sheep", Name = "Sheep",
                Walk = 1.0f, Trot = 1.7f, Run = 4.4f, Notice = 14f, CalmSpeed = World.SneakSpeed + 0.6f,
                HomeRange = 12f, FleeAhead = 6f, WanderMin = 3f, WanderMax = 9f, WoodPref = 0f, Curious = true,
                Shearable = true, Bleats = true,
                Meat = Item.RawMutton, MeatGrown = 3, MeatYoung = 1, Hide = Item.SheepHide,
                Model = "sheep",
                // One flock at the edge of the clearing where players wake up, the rest further out.
                Herds = new[] { new AnimalHerdSpot(4f, 3f, 5, 1f), new AnimalHerdSpot(-34f, 22f, 6, 14f), new AnimalHerdSpot(30f, -36f, 4, 14f), new AnimalHerdSpot(-42f, -30f, 5, 14f) },
            },
            // Red deer (animals-14): notice you from far off, and even a careful approach
            // unsettles them; they keep to the woodland edge and bolt far into the trees. A
            // hunter waits, still, for deer to graze near (a still player never alarms them).
            new()
            {
                Id = Deer, Key = "animal.deer", Name = "Red deer",
                Walk = 1.1f, Trot = 2.4f, Run = 6.5f, Notice = 30f, CalmSpeed = 1.2f,
                HomeRange = 18f, FleeAhead = 12f, WanderMin = 4f, WanderMax = 12f, WoodPref = 0.45f, Curious = false,
                Wary = 10f, RunFloor = 0.3f, RunHealthShare = 0.7f, RestBelowHealth = 45,
                Meat = Item.RawVenison, MeatGrown = 2, MeatYoung = 1, Hide = Item.DeerHide, Extra = Item.Antler,
                Model = "deer", AntlerShare = 0.35f,
                Herds = new[] { new AnimalHerdSpot(70f, 45f, 4, 22f), new AnimalHerdSpot(-60f, -75f, 3, 22f) },
            },
            // Hens (animals-19): a tame yard flock that stays close to where it lives, scratches about
            // and shies only from a charge. D-075: an egg every game day and a half, three held.
            new()
            {
                Id = Hen, Key = "animal.hen", Name = "Hen",
                Walk = 0.7f, Trot = 1.3f, Run = 3.4f, Notice = 7f, CalmSpeed = 2.4f,
                HomeRange = 7f, FleeAhead = 4f, WanderMin = 1f, WanderMax = 4f, WoodPref = 0f, Curious = true,
                Meat = Item.RawChicken, MeatGrown = 1, MeatYoung = 1, Hide = null, Extra = Item.Feather,
                Egg = Item.Egg, EggDays = 1.5f, EggCap = 3, Roosts = true,
                Model = "hen",
                Herds = new[] { new AnimalHerdSpot(-8f, 7f, 4, 4f) },
            },
        };

        public static AnimalDef Get(byte species) => All[Math.Clamp((int)species, 0, All.Length - 1)];
    }
}
