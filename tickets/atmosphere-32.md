# atmosphere-32: Smoke signal from a stoked fire
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: done
Branch: claude/atmosphere-32-smoke-signal
PR: #524 (merged); far signals #533

## Goal
From a playtester, via Justin: "if a fire is above 75%, make it smoke more ... I want to be able to see like a smoke signal kinda thing." A fire whose fuel is above 75% of the most a fire can hold (`Fires.MasterMaxSeconds`, or that times `Structures.HearthFuelScale` for a fire on a raised hearth) puts up a tall, thick, soft smoke column, about 30 to 36 m, leaning with the wind and spreading at the top, readable from as far as the camera sees (300 m). Below 75% the fire keeps today's light smoke (AtmosphereHomes). Between 70% and 80% the column swells and thins smoothly. Campfires send it up from the fire; a home sends it up from its smoke outlet (chimney) when the fire inside is stoked. Client only, no server change. Justin sees a pale, painted, sun-lit column over a stoked camp, warm at the foot, grey and wide higher up.

## Progress log
- 2026-10-17: built `client/scripts/atmosphere/AtmosphereSmokeSignal.cs` (partial of Atmosphere), hooked with `ReadySignals()` / `UpdateSignals()` in Atmosphere.cs. Per fire, fraction = fuel left / most it can hold; signal = smoothstep(0.70, 0.80, fraction). Up to 6 columns, nearest the player first, within 320 m; each column stays on its own fire and fades in or out over about 3 s. Near: 40 lit billboard puffs per column (StandardMaterial3D, lambert-wrap diffuse + backlight, procedural lumpy puff texture, proximity fade, 16 s life, damped rise, wind drift via gravity), drawn within 110 m, cross-fading out between 50 and 75 m. Far: one camera-facing strip per column with a small spatial shader (bends downwind, rising noise billows, lit, faint warm glow at the foot at night), cross-fading in from about 45 to 70 m. `--smoke-signal=1` forces every fire to full; with no live fire nearby it raises one column at the `--screenshot-fire` spot (or 12 m ahead of the camera). `dotnet build`: 0 errors. Not run in Godot (no binary in the session).

- 2026-10-17 05:00: Justin answered via the PM. (1) The threshold is 75% of the lighter's OWN cap, so anyone can signal; asked Crafting (session_01N9APthjRDfjknUvCweqzzN) to publish the fire's cap on the Fire row, and `FireMost()` uses the Master cap until then. (2) Never from homes or indoor hearths: fires inside a doorway structure are skipped and the chimney column is gone; outdoor raised hearths still signal, with the hearth bonus. Integration is looking at the 300 m far plane.

- 2026-10-17 05:15: far signals (atmosphere-32b, branch claude/atmosphere-32b-signal-server, built on #524). Server: `AtmosphereSignals.cs` adds a public `SignalFire {FireId, X, Z, Until}` table, refreshed on the weather tick (20 s) by `SweepSignalFires`. A fire is listed when it's not inside a doorway home and holds more than 75% of its cap, with Until = FuelUntil − 0.75 × cap. Client: columns for SignalFire rows beyond the fires the client holds, out to 1500 m, fading over the last 10 s before Until. Integration adds the world-wide subscription and the ~1500 far plane. Server and client build with 0 errors.

- 2026-10-17 05:20: draft PR #533. Integration agreed the table stays in my AtmosphereSignals.cs, server-written only, and that I add `client/scripts/net/Subscriptions.Atmosphere.cs` ([SubscriptionQueries] AtmosphereQueries yielding "SELECT * FROM signal_fire") once their #527 lands; their #529 is the 1500 m far plane. Crafting's #531 adds `Fire.FuelCapSeconds`: the lighter's cap including the hearth scale, raised to a feeder's higher cap, and 0 on old fires (fall back to Fires.MaxSeconds). The rule is (FuelUntil − now) >= 0.75 × FuelCapSeconds.

- 2026-10-17 05:30: per Integration, `SELECT * FROM signal_fire` is a plain core query in Subscriptions.cs for now (Integration moves it to Subscriptions.Atmosphere.cs when #527 lands). tools/upgrade-check.sh origin/main passed locally: the new table migrates without a wipe.

- 2026-10-17 04:48: #524 merged (green sha d64d487a, CI run 39003198556). Near columns are live. #533 carries the far signals.

- 2026-10-17 05:50: #531 merged; main merged in. Server FireCapSeconds and client FireMost now read Fire.FuelCapSeconds (0 → Fires.MaxSeconds), with no hearth multiply of my own. #533 is out of draft. Server and client build with 0 errors.
- 2026-10-17 08:01: CI green on 0b05808a (full run, upgrade check included); merged as #533.

## Open questions / blockers
- Design (for Justin via PM): the threshold is 75% of the Master cap (40 min). A new player's fire tops out at 20 min (50%), so only a mid-rank Firecraft player can raise a signal. Options: (a, recommended for now, as built) keep 75% of the Master cap, so a signal is something you grow into; (b) 75% of the lighter's own cap, which needs the server to publish the cap or rank. Also, a hearth fed from nearby baskets (D-149) stays near its cap, so a well-stocked home of a ranked player would signal all the time.
- The camera far plane is 300 m and depth fog (0.006) thins the far column past about 200 m; neither is this lane's file.

## Next step
None (done). If Integration asks, move the signal_fire query into Subscriptions.Atmosphere.cs.
