# claims-13: Server-authority audit of the Claims lane (D-182)
Lane: Claims, chat and PvP rules
Owner: Claims lane (Sonnet session)
Status: in_progress
Branch: claude/claims-13-authority
PR: #315

## Goal
Every Claims, Chat, Trading, stash and fighting reducer validates reach, ownership, cooldowns and limits on the server, and no public table leaks values that should stay hidden. Gaps get closed in small PRs with SocialTest checks.

## Progress log
- 2026-10-16: static read of Claims.cs, Chat.cs and Trading.cs (no dotnet in this sandbox, so nothing was run).
  - Sound: TendClaim, stash/take (RequireStashAccess: standing claim + rights + near marker), owner-only reducers (RequireOwnClaim / RequireOwnStandingClaim), SetClaimLifetime (admin only), trading (distance, one trade each, version check, re-check on swap, caps), chat cooldown and length.
  - Private tables (good): ClaimStash, ClaimStashLog, Trade, TradeItem, ChatState.
  - Gap A: `ChatMessage` is a public table. Every client can read every message, whispers included, and with its X/Z. Range, voice and chunk filtering happen only on the client (Subscriptions.cs chunk queries). Options to fix: see below.
  - Gap B (small): `LeaveClaim` and `RemoveClaimMember` do nothing and don't complain when there is nothing to remove. Harmless, no change planned.
  - Still to read: Combat.cs (strike reach, cooldown, PvP check), Stance.cs, ClaimNames.cs.

- 2026-10-16: PM picked (b). Implemented: private `ChatHeard` table (one row per listener in range, speaker included) + public `MyChat` view; SendChat and SayNearby go through `Deliver`; SocialLayer subscribes to `my_chat`; SocialTest reads MyChat and checks an out-of-earshot whisper never arrives. `ChatMessage` is now private and unwritten (kept so existing worlds upgrade without a wipe; PruneChat still clears it). Not built locally (no dotnet); CI is the first build, and its upgrade check covers the schema change.

## Open questions / blockers
- Gap A: PM chose (b), done (see log). Options were: (a) keep as is, since chat is only ever near-field and a hostile client can read the lot (cheap, leaves the leak); (b) server-side range: move whispers and shouts out of the public table into a per-recipient private table written by SendChat for players in range [recommended; more writes per message, 0 client leak]; (c) a SpacetimeDB client visibility filter by chunk (smallest code, depends on filter support in our SDK version, unverified).

## Next step
Open the PR, wait for CI (check the upgrade check passes without a wipe). Then read Combat.cs, Stance.cs and ClaimNames.cs for the same checks and add findings here.
