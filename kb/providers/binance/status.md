---
title: Binance provider status
type: provider-status
status: living
created: 2026-09-01
---

# Provider status — binance

> **Living. Changes when we do work.** The contract itself is in `manifest.md`, deliberately apart.

## Meta

- provider: `binance`; market types: spot, usd-futures
- manifest: [`manifest.md`](manifest.md)
- docs revision: spot `a0057759f1cbcab812af44b75309d72866a57561`; futures fetched 2026-09-01 (no
  repository exists, so the date is the only anchor)
- working branch: `main` where converged
- last reconciled: 2026-09-01 — steps 1-2, incomplete; drift found

## Convergence

| step | state | evidence | outstanding |
|---|---|---|---|
| 1 — derive existing state | **converged** | ~70 anchors verified and re-anchored after the environment removal; four missing entries added; both axes censused entry by entry against the test suites | none |
| 2 — collect facts, compute drift | **drift** | all 13 futures pages and 7 spot files snapshotted; every category walked | request side closed at tier 1 by the official Postman collections; response side read at tier 3, lossy. Remaining: the nested user-data-stream event payloads (~20 field names) reached by no technique, and one unresolved item — whether `POST /fapi/v1/order` returns `avgPrice`, which our converter reads. **This blocks: the step may not be declared done partially** |
| 3 — wire types and serialization | not-started | — | — |
| 4 — provider, read paths (+ registration, config, read-only live validation) | not-started | — | **the futures WebSocket base URLs are legacy, decommissioned 2026-04-23** — market to `/public`, user data to `/private` |
| 5 — connector, streams and orders (+ registration, config, trading live validation) | not-started | — | blocked on the same URL drift: a user stream on a dead URL delivers nothing |

## Queued work

Named here rather than left implied, with the reason each is not being done now.

- **Rebuild the signing golden value.** The fixture's query — `symbol=LTCBTC&side=BUY&…` — contains no
  character requiring percent-encoding, so it passes whether or not the implementation encodes before
  signing, which Binance has required since 2026-01-15. The test is `vacuous` for that property. Not
  rebuilt yet: a live signed request settles it either way, so if the exchange stages pass, the
  implementation is right and only the test needs strengthening; if they fail with `-1022`, the fix is
  the implementation and the test is the second job, not the first.
- **Rate-limit handling in the runtime** — 418 folded into the same status as 429, `Retry-After` read
  nowhere, and a limiter that throttles only on its own accounting. Deliberately deferred until a live
  read-only run shows whether we approach the limits at all, so the backoff is designed against
  observation rather than documentation.
- **Two `vacuous` tests**, both of the same shape — the input chosen cannot exercise the property the
  test claims. The signing golden value, above; and the history paging tests, which request one day
  while claiming to protect a seven-day window and a three-month cap. Neither is fixed here: the first
  waits on the live run, the second belongs to the step that owns the read paths.
- **Five components with no test file at all** — `WebSocketService`, `ListenKeyResolver`,
  `HttpRequestSignatureExtensions`, `HttpRequestLogExtensions`, and the filter converters. The first
  two carry the connection lifecycle of every stream this module runs.
- **The read-side enumeration gaps** — most order-type and order-status wire strings are never parsed
  by any test, only written. Work for the step that owns serialization.

## Reconcile history

One line per run. The report holds the findings; the snapshot beside it holds the documentation those
findings were read from.

| date | layer | report | outcome |
|---|---|---|---|
| 2026-09-01 | 1-2 — contract | *(derived from code, no report)* | manifest inventoried; `checked_against: never` |
| 2026-09-01 | 1-2 — contract | [`2026.09/2026.09.01-contract.md`](2026.09/2026.09.01-contract.md) | **blocking drift**: futures WebSocket URLs decommissioned. Step 1 converged; step 2 complete but for the futures endpoint schemas. One unverified assumption settled in our favour; the sandbox environment removed from the code entirely |
