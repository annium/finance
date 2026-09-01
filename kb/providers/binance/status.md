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
- docs revision: none pinned — the first contract run records the spot commit SHA and the futures
  fetch date
- working branch: `main` where converged
- last reconciled: never

## Convergence

| layer | state | evidence | outstanding drift |
|---|---|---|---|
| 1 — contract | **drift** | manifest derived from code | every entry `[UNVERIFIED]`: never compared against Binance's documentation |
| 2 — contracts and converters | not-started | — | — |
| 3 — provider | not-started | — | — |
| 4 — connector | not-started | — | — |
| 5 — registration and configuration | not-started | — | — |
| 6 — live validation | not-started | no stage has run; no fact is `[LIVE]` | stages 6a-6d all pending |

## Reconcile history

One line per run. The report holds the findings; the snapshot beside it holds the documentation those
findings were read from.

| date | layer | report | outcome |
|---|---|---|---|
| 2026-09-01 | 1 — contract | *(derived from code, no report)* | manifest inventoried; `checked_against: never` |
