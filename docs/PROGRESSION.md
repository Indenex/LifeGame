# Progression curves: how to pick numbers

Reference for every lane proposing a number (D-075). Pick a curve family below, state its parameters and where your number sits on it, and send the proposal to the PM. Justin decides anything players will notice.

House rules this doc serves:

- **Start low, grow slowly (D-075).** Gains come faster through roughly the first 15% of progression, then a long slow climb.
- **Fun first, light survival (D-081).** Needs nudge, never punish. No death spirals.
- **Skill mastery about 6 hours for pre-alpha (D-086)**, much longer later. Effects per rank stay linear for now.

## 1. What other games do

| Pattern | Example | What it teaches us |
|---|---|---|
| Exponential cost per level | OSRS: each level costs ~10% more XP than the last; level 92 (6,517,253 XP) is half of 99 (13,034,431) [1] | The last levels take half the total time. Good for a long tail, harsh if the reward per level stays flat. |
| "Halfway is late" | Diablo IV: level 85 is about halfway to 100 [2] | Players accept it if early levels feel quick. |
| Exponential cost, linear output | Idle games: cost = base x 1.07^owned (AdVenture Capitalist), output grows linearly [3] | Cost rate 1.07 to 1.15 per step is the usual range; it makes every next step take longer without any one step feeling like a wall. |
| Diminishing returns per point | First stat point +10, next +9, next +8 [4]; "modified power curve: early fast, mid moderate, late long" [4] | Front-loads the value of each investment. |
| Equal benefit, rising time | EVE Online: each skill level gives the same benefit but takes longer [5] | Same as our setup: linear effect per rank, front-loaded gain speed. |
| Variable-ratio rewards | Skinner's schedule: reward after an unpredictable number of actions gives steady, high response [6][7] | Random drops keep actions interesting. Use for small delights, never for anything that feels like gambling. |
| Flow channel, sawtooth | Challenge vs skill: too hard is anxiety, too easy is boredom; alternate tension and mastery in a sawtooth [8][9] | After each step up, give a stretch where the player feels strong before the next push. |
| Max-energy growth | Stardew: 270 energy, up to 508 with all 7 Stardrops (+88% over the whole game) [10] | A survival-ish bar can roughly double over a full playthrough and still feel earned. |

Stamina and sprint references:

| Game | Sprint | Refill | Growth | Source |
|---|---|---|---|---|
| Valheim | 50 base stamina, run costs ~8/s, so ~6 s of running | Regen after ~1 s pause, 12/s when empty down to 6/s near full: ~5 to 6 s | Run skill lowers cost to about two-thirds at 100; food raises the pool | [11][12] |
| Minecraft | Sprint ends on its own after 30 s; needs food above 6 | Food, not a timer; 0.1 exhaustion per meter | Food only | [13] |
| Elden Ring | 80 stamina at Endurance 1 | ~45/s; full bar in ~4 s | Endurance stat | [14][15] |
| Zelda BotW | Steady drain while sprinting; empty wheel slows Link for a few seconds | Constant rate | Extra wheels from shrines; food buffs | [16] |
| Don't Starve | No stamina; hunger, health and sanity drain over a day cycle | Food | Gear | (general knowledge) |

Takeaway: action games use short sprints with fast refills (a few seconds each). Cosy and exploration games either have no sprint limit or tie it loosely to food. For a light need, LifeGame should sit between: long enough to cross a clearing, short enough that Shift is a choice.

## 2. Curve families

`p` is progress from 0 to 1 (quality range, rank, tier, hours played). `lo` and `hi` are the start and end values. All families below are tuned so **15% of progress gives about 40% of the gain**, which is D-075 expressed as a number.

### A. Saturating (fast then flat)

`value(p) = lo + (hi - lo) * (1 - e^(-k p)) / (1 - e^(-k))`, with **k = 3.2** for the 15%-gives-40% fit.

Use for: things with a natural ceiling that should feel nearly done well before the end (fire length by gear, carry by container quality). Strongest flattening of the four; the last 25% adds only ~5%.

### B. Logarithmic (the "carrot")

`value(p) = lo + (hi - lo) * ln(1 + b p) / ln(1 + b)`, with **b = 12** for the fit.

Use for: long open-ended ladders where each step should still be worth a bit (yields, insulation, tool tiers). Flattens more gently than A, so late steps still matter.

### C. Power (square-root-like)

`value(p) = lo + (hi - lo) * p^a`, with **a = 0.48** for the fit; a = 0.7 for a milder version.

Use for: the simplest front-loaded curve; easy to explain and tune with one number. Largest early jump (5% of progress gives 24%).

### D. Piecewise "15/40" (two straight lines)

`value(p) = lo + (hi - lo) * 0.4 * p/0.15` for p up to 0.15, then `0.4 + 0.6 * (p - 0.15)/0.85`.

Use for: anything players read as a table (tier lists, wiki). Predictable, no hidden math. Late steps feel the most even.

### E. Exponential cost (for what a step costs, not what it gives)

`cost(n) = base * r^n`, with r = 1.05 to 1.10 for skills (OSRS uses ~1.10), 1.07 to 1.15 for buildings or upgrades.

Use for: XP per rank or materials per upgrade, paired with a linear benefit. The pairing gives front-loaded progress in time, like EVE and OSRS.

### Worked table (share of the total gain)

| Progress p | A saturating | B log | C power | D piecewise |
|---|---|---|---|---|
| 0 | 0% | 0% | 0% | 0% |
| 0.05 | 16% | 18% | 24% | 13% |
| 0.10 | 29% | 31% | 33% | 27% |
| 0.15 | 40% | 40% | 40% | 40% |
| 0.25 | 58% | 54% | 51% | 47% |
| 0.50 | 83% | 76% | 72% | 65% |
| 0.75 | 95% | 90% | 87% | 82% |
| 1.00 | 100% | 100% | 100% | 100% |

### Do not front-load twice

Our skill gain is already front-loaded in time (`Skills.Gain` in `shared/Skills.cs`: 100 x (1 - p)^2 points per use, floor 5). 15% of the uses to Master reaches **rank ~55**. So an effect that is linear in rank (D-086) already gives about 55% of its gain in the first 15% of play time. Putting curve A on top of rank would give ~85% in that time and leave nothing to chase. Rule: **if the input is skill rank, keep the effect linear in rank.** Use curves A to D when the input is item quality, gear tier or a count.

## 3. Rewards between the steps (variable ratio, light)

Small random bonuses make repeated work pleasant: a second clay lump (already `SecondLumpChance`, up to 50% at Master), a rare pretty stone, a better-than-expected quality roll. Suggested base: a "nice surprise" about 1 in 15 to 1 in 30 actions, cosmetic or small. Never random losses, never anything bought, never a streak that resets.

## 4. Suggestion bases for LifeGame now

Recommended option first. Numbers are proposals for the PM and Justin.

### Stamina (D-088)

Assumes running at about 6 m/s (1.4x the 4.2 m/s walk). Refill starts 1 s after you stop running and is faster when the bar is low (Valheim style), so you never wait long at the top.

| Option | Start: run / full refill | Top: run / full refill | Grows with | Trade-off |
|---|---|---|---|---|
| **1. Recommended** | **8 s (~48 m) / 12 s** | **16 s / 7 s** | Survival rank, linear (rank is already front-loaded); a meal gives +25% run for 10 min | Shift is a real choice early, comfortable at Master; doubling matches Stardew's ~+88% energy. |
| 2. Generous | 12 s / 10 s | 20 s / 6 s | Same | Less frustration on a big map, but less reason to improve it. |
| 3. Action-game | 6 s / 6 s | 10 s / 4 s | Same | Snappy and Valheim-like, but stop-start running feels like a chore in a cosy game. |

By Survival rank (option 1): rank 0: 8 s / 12 s; 20: 9.6 / 11; 40: 11.2 / 10; 60: 12.8 / 9; 80: 14.4 / 8; 100: 16 / 7. Running out drops you to walking, nothing else (D-088).

### Tool speed bonus, 0 to 15% (D-089)

Input is tool quality 1 to 100. Note a novice can already make quality 35 (`QualityCap`), so the curve must not give most of the bonus there.

| Quality | 1. Power a=0.7 (rec.) | 2. Saturating k=3.2 | 3. Linear |
|---|---|---|---|
| 15 | 3.8% | 5.7% | 2.1% |
| 25 | 5.6% | 8.5% | 3.6% |
| 35 | 7.1% | 10.5% | 5.2% |
| 50 | 9.2% | 12.5% | 7.4% |
| 70 | 11.7% | 14.0% | 10.5% |
| 85 | 13.4% | 14.6% | 12.7% |
| 100 | 15.0% | 15.0% | 15.0% |

1. **Power a=0.7**: early tools feel better, and a Master's tool is still clearly worth having.
2. Saturating: a novice tool already gives 70% of the bonus, so quality hunting stops mattering.
3. Linear: easiest to read, but early tools feel identical.

### Clothing insulation toward ~50% (D-075 first case)

Input is clothing tier or set progress. Start well under 25% (D-075).

| Option | Ladder | Trade-off |
|---|---|---|
| **1. Log ladder (rec.)** | grass cape 5%, bark/fiber wrap 10%, hide wrap 18%, hide + fiber layers 26%, woven wool 34%, wool + leather set 42%, best fitted set 50% | Each new garment is a visible step; late steps still add 8%. |
| 2. Saturating by quality | `5 + 45 x A(p)`: p 0.15 gives 23%, p 0.5 gives 43% | Smooth, but most warmth comes early and late gear feels thin. |
| 3. Per-slot sum | 4 slots (head, body, legs, feet), each 2% to 12.5% by quality | Rewards collecting a set; more UI and balance work. |

### Carry capacity (D-042, D-052)

Today: 20 bare hands, basket +15, half speed at 1.5x the limit. Keep the body flat (realism) and let gear carry the growth.

| Option | Ladder | Trade-off |
|---|---|---|
| **1. Containers grow by quality (rec.)** | basket +8 at low quality to +15 at high (curve A); later back frame +15 to +25; later pack animal | Rewards better weaving, keeps the body honest, total ~3x by the late game. |
| 2. Survival rank adds strength | +0 to +5, linear in rank, plus containers | Every player gains a bit; blurs the "build a basket" goal. |
| 3. Flat basket +15 (as now) | No growth until new containers | Simplest; no reason to make a better basket. |

### Skill pace to ~6 h mastery (D-086)

The current gain formula needs ~794 uses for Master (11 for rank 10, 100 for rank 50). At ~27 s per use that is 6 hours.

| Hours | 0.25 | 0.5 | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|---|---|---|
| Rank (current formula) | 24 | 39 | 57 | 72 | 80 | 86 | 93 | 100 |

| Option | How | Trade-off |
|---|---|---|
| **1. Keep the formula, check use time (rec.)** | Keep `Gain`; per skill, check average seconds per use and adjust points per use so 794 uses take ~6 h | Visible gains in session one (M2); title toasts at 20/40 arrive in the first hour. |
| 2. Gentler start | Gain = 70 x (1 - p)^2, floor 10 (~612 uses; rank 8 after 12 uses, 41 after 100) | 15% of time gives ~39% of ranks, D-075's letter; the first hour is flatter and the tail shorter. |
| 3. Scale for later | Multiply total uses (e.g. x3.3 for 20 h, M4) by lowering the floor and base together | Same shape at any length; do this later, not now. |

### Fire duration 20 to 40 min with Firecraft (D-090)

Input is Firecraft rank, so follow the "do not front-load twice" rule.

| Rank | 0 | 10 | 20 | 40 | 60 | 80 | 100 |
|---|---|---|---|---|---|---|---|
| 1. Linear (rec.) | 20 | 22 | 24 | 28 | 32 | 36 | 40 min |
| 2. Saturating on rank | 20 | 25.8 | 29.9 | 35.1 | 37.8 | 39.3 | 40 min |
| 3. Steps at titles | 20 | 20 | 24 | 28 | 32 | 36 | 40 min |

1. **Linear**: follows D-086; about 31 min after one hour of Firecraft.
2. Saturating: 30 min within ~30 minutes of play, then almost nothing to chase.
3. Steps at titles: ties to the title toast (D-067), but no change between titles.

## 5. How to propose a number

1. Name the input (rank, quality, tier, count) and the range `lo` to `hi`.
2. If the input is rank: linear. Otherwise pick A to D and say why.
3. Give a 5 to 7 row table like the ones above and where your value sits (for example, "rank 20, 24 min, 20% of the gain").
4. Check the start value against "low" and the 15% value against "about 40%".

## Sources

1. OSRS Wiki, Experience table: https://oldschool.runescape.wiki/w/Experience_table
2. TweakTown, Diablo IV level 85 is halfway to 100: https://www.tweaktown.com/news/91936/diablo-iv-gamers-relate-to-runescape-players-after-realizing-level-85-is-halfway-100/
3. Anthony Pecorella, The Math of Idle Games, Part I (Game Developer): https://www.gamedeveloper.com/design/the-math-of-idle-games-part-i
4. StraySpark, RPG stat systems and character progression design: https://www.strayspark.studio/blog/rpg-stat-systems-character-progression-design
5. Elder Scrolls Online forums, diminishing returns of the CP system (EVE comparison): https://forums.elderscrollsonline.com/en/discussion/comment/1583557/
6. FourWeekMBA, Variable ratio schedule: https://fourweekmba.com/variable-ratio-schedule/
7. The Thoughtful Gamer, Gloomhaven and the Skinner Box: https://thethoughtfulgamer.com/2018/06/04/gloomhaven-and-the-skinner-box/
8. Game Developer, Getting gamers in the zone: understanding flow: https://www.gamedeveloper.com/design/getting-gamers-in-the-zone-understanding-flow
9. Flow (video game), based on Jenova Chen's thesis Flow in Games: https://en.wikipedia.org/wiki/Flow_(video_game)
10. Stardew Valley Wiki, Max Energy: https://stardewvalleywiki.com/Max_Energy
11. Valheim Wiki, Stamina (mirror): https://breezewiki.discard.no/valheim/wiki/Stamina
12. ProGameTalk, Valheim run skill: https://progametalk.com/valheim/how-to-level-up-run-skill/ (secondary source for the 8/s cost)
13. Minecraft Wiki, Sprinting and Hunger: https://minecraft.wiki/w/Sprinting, https://minecraft.wiki/w/Hunger
14. Elden Ring Wiki (Fextralife), Stamina: https://eldenring.wiki.fextralife.com/Stamina
15. Steam community, Elden Ring stamina regen timing (user test): https://steamcommunity.com/app/1245620/discussions/0/4406291873006594116
16. Zelda Wiki, Stamina Wheel: https://zeldawiki.wiki/wiki/Stamina_Wheel

Curve parameters and tables were computed from the formulas above; skill-pace numbers come from `Skills.Gain` in `shared/Skills.cs`.
