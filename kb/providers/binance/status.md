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
| 1 — derive existing state | **drift** | manifest derived 2026-09-01 | anchors were never verified against the code; the derivation was taken on trust from the sweep that produced it |
| 2 — collect facts, compute drift | **drift** | partial pass against the 2026-09-01 snapshot | not every entry has an outcome — the high-value categories were walked, the rest were not. Futures per-endpoint reference pages unretrievable; the `.md` trick does not work for the `/catalog/` routes, so a different technique is needed. **Both block: this step may not be declared done partially** |
| 3 — wire types and serialization | not-started | — | — |
| 4 — provider, read paths (+ registration, config, read-only live validation) | not-started | — | **the futures WebSocket base URLs are legacy, decommissioned 2026-04-23** — market to `/public`, user data to `/private` |
| 5 — connector, streams and orders (+ registration, config, trading live validation) | not-started | — | blocked on the same URL drift: a user stream on a dead URL delivers nothing |

## Reconcile history

One line per run. The report holds the findings; the snapshot beside it holds the documentation those
findings were read from.

| date | layer | report | outcome |
|---|---|---|---|
| 2026-09-01 | 1-2 — contract | *(derived from code, no report)* | manifest inventoried; `checked_against: never` |
| 2026-09-01 | 1-2 — contract | [`2026.09/2026.09.01-contract.md`](2026.09/2026.09.01-contract.md) | partial baseline; **blocking drift**: futures WebSocket URLs decommissioned. One unverified assumption settled in our favour |
