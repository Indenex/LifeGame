"""2-hour building session against the real survival rules (shared/Needs.cs, Survival.cs).
Not a game run: a 1 s step model of one builder who works away from the hearth and returns
when a need falls to the Low nudge (0.25). Constants copied from source."""
FOOD_DRAIN = 1 / (90 * 60)
WATER_DRAIN = 1 / (60 * 60)
COLD_DRAIN = 1 / (16 * 60)
FIRE_HEAT = 1 / 60          # per second, right beside a burning fire
WARM_RADIUS = 5.0           # campfire: warmth falls linearly to 0 at 5 m (Fires.WarmthRadius)
STONE_RING_RADIUS = 9.0     # stone-ringed hearth (Fires.StoneRingWarmthRadius)
LOW = 0.25
MAX_CHILL = 2.0
SIP = 0.2; SKIN = 0.4; COIL = 1.0
ROAST = 0.40; BERRIES3 = 0.36  # one bush pick gives 3 berries at 0.12 each
STEW = 0.65


def air_cold(c):
    return min(max((15 - c) / 15, 0), 1)


def chill(c):
    return min(1 + air_cold(c), MAX_CHILL)


def run(celsius, work_dist, water_item, food_item, secs=7200, start=(0.8, 0.8, 1.0), radius=WARM_RADIUS):
    food, water, warm = start
    away = 0; at_fire = 0
    meals = drinks = trips = 0
    first_meal = first_drink = None
    cold = COLD_DRAIN * chill(celsius)
    at_hearth = False
    for t in range(secs):
        d = 1.5 if at_hearth else work_dist
        heat = max(0.0, 1 - d / radius) * FIRE_HEAT
        food -= FOOD_DRAIN; water -= WATER_DRAIN
        warm = min(1.0, warm + heat - cold)
        warm = max(warm, 0)
        if at_hearth: at_fire += 1
        else: away += 1
        if water < LOW:
            water = min(1.0, water + water_item); drinks += 1
            if first_drink is None: first_drink = t / 60
        if food < LOW:
            food = min(1.0, food + food_item); meals += 1
            if first_meal is None: first_meal = t / 60
        if not at_hearth and warm < LOW:
            at_hearth = True; trips += 1
        elif at_hearth and warm >= 0.95:
            at_hearth = False
    return dict(meals=meals, drinks=drinks, trips=trips, warmups_per_h=trips / (secs / 3600),
                pct_at_fire=100 * at_fire / secs, first_drink_min=first_drink, first_meal_min=first_meal,
                min_food=None)


if __name__ == "__main__":
    cases = [("mild day 15C (spring/summer/autumn day)", 15),
             ("spring/autumn night 9C", 9),
             ("winter day 14C", 14),
             ("winter night -8C", -8)]
    for name, c in cases:
        print(f"\n{name}: chill x{chill(c):.2f}, a bar of warmth lasts {16 / chill(c):.1f} min away from a fire")
        be = WARM_RADIUS * (1 - (COLD_DRAIN * chill(c)) / FIRE_HEAT)
        print(f"  break-even distance from the hearth: {be:.1f} m (closer gains warmth, further loses it)")
        for dist in (8, 15):
            r = run(c, dist, SKIN, ROAST)
            print(f"  work {dist:>2} m off, skin+roast: meals {r['meals']}, drinks {r['drinks']}, warm-ups/h {r['warmups_per_h']:.1f}, "
                  f"{r['pct_at_fire']:.0f}% of time at the fire")
    print("\nStone-ringed hearth (9 m) vs campfire (5 m), winter night -8C, builder working at distance d:")
    for d in (4, 6, 7, 8, 10):
        a = run(-8, d, SKIN, ROAST)
        b = run(-8, d, SKIN, ROAST, radius=STONE_RING_RADIUS)
        print(f"  {d:>2} m: campfire {a['warmups_per_h']:.1f} warm-ups/h ({a['pct_at_fire']:.0f}% at fire)   stone ring {b['warmups_per_h']:.1f} warm-ups/h ({b['pct_at_fire']:.0f}% at fire)")
    be = STONE_RING_RADIUS * (1 - (COLD_DRAIN * chill(-8)) / FIRE_HEAT)
    print(f"  break-even distance, winter night: campfire {WARM_RADIUS * (1 - (COLD_DRAIN * chill(-8)) / FIRE_HEAT):.1f} m, stone ring {be:.1f} m")
    print()
    for label, w, f in (("sips (0.2)", SIP, ROAST), ("skin (0.4)", SKIN, ROAST), ("coil pot (1.0)", COIL, STEW)):
        r = run(15, 15, w, f)
        print(f"2 h, mild, {label}: drinks {r['drinks']}, meals {r['meals']}, first drink {r['first_drink_min']:.0f} min, first meal {r['first_meal_min']:.0f} min")
    print()
    print("Totals over 2 h with no refills at all: food", f"{7200 * FOOD_DRAIN:.2f}", "bars, water", f"{7200 * WATER_DRAIN:.2f}", "bars")
    print("Minutes a full bar lasts: food", 1 / FOOD_DRAIN / 60, "water", 1 / WATER_DRAIN / 60)
    print("Minutes from full to the Low nudge: food", 0.75 / FOOD_DRAIN / 60, "water", 0.75 / WATER_DRAIN / 60)
    print("Minutes a single item buys: sip", SIP / WATER_DRAIN / 60, "skin", SKIN / WATER_DRAIN / 60, "coil", COIL / WATER_DRAIN / 60,
          "| berries(1 pick)", BERRIES3 / FOOD_DRAIN / 60, "roast", ROAST / FOOD_DRAIN / 60, "stew", STEW / FOOD_DRAIN / 60)
