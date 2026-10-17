# bugfix-43: Tokens per server, and a refused token joins fresh
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-43
PR: (see PR list)

## Goal
A tester who first ran the host launcher (own local world) got a 401 joining Justin's server because Main.cs reused user://token.txt, issued by another SpacetimeDB server. Tokens are now stored per server in `user://token_<host>_<port>[_<profile>].txt`; an old `token.txt` (with profile suffix) is tried once when the per-server file doesn't exist. A 401/Unauthorized refusal while a token was sent sets the token aside as `.bak`, shows a calm note and reconnects once without a token. Client only.

## Progress log
- 2026-10-17: implemented in Main.cs (Connect(useToken), ConnectFailed, RetireToken). Tested headless on a local server with a bad token: it was set aside as .bak, joined fresh, and a new per-server token was saved.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the green sha to the PM.
