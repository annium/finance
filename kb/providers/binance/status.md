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
- last reconciled: 2026-09-01 — layer 1, drift found

## Convergence

| layer | state | evidence | outstanding drift |
|---|---|---|---|
| 1 — contract | **drift** | baseline established against the 2026-09-01 snapshot | futures per-endpoint reference pages unretrievable, so futures request/response schemas remain unverified against their own pages |
| 2 — contracts and converters | not-started | — | — |
| 3 — provider | not-started | — | — |
| 4 — connector | not-started | — | — |
| 5 — registration and configuration | **drift** | — | **the futures WebSocket base URLs are legacy and were decommissioned 2026-04-23**: market must move to `/public`, user data to `/private`. Blocks stages 6b and 6d |
| 6 — live validation | not-started | no stage has run; no fact is `[LIVE]` | 6a and 6c unblocked; **6b and 6d blocked** on the layer-5 drift above |

## Reconcile history

One line per run. The report holds the findings; the snapshot beside it holds the documentation those
findings were read from.

| date | layer | report | outcome |
|---|---|---|---|
| 2026-09-01 | 1 — contract | *(derived from code, no report)* | manifest inventoried; `checked_against: never` |
| 2026-09-01 | 1 — contract | [`2026.09/2026.09.01-contract.md`](2026.09/2026.09.01-contract.md) | baseline established; **blocking drift**: futures WebSocket URLs decommissioned. One unverified assumption settled in our favour |
