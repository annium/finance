# Snapshot sources — 2026-09-01

What this run read. The report beside it was written from these files, not from the live sites.

## spot — tier 1 (upstream git repository)

Repository `github.com/binance/binance-spot-api-docs`, pinned at
**`a0057759f1cbcab812af44b75309d72866a57561`** (master, 2026-09-01).
Fetched with `curl -sSL https://raw.githubusercontent.com/binance/binance-spot-api-docs/<sha>/<path>`.

| file | bytes |
|---|---|
| `spot/CHANGELOG.md` | 131748 |
| `spot/enums.md` | 5244 |
| `spot/errors.md` | 20321 |
| `spot/filters.md` | 13528 |
| `spot/rest-api.md` | 181189 |
| `spot/user-data-stream.md` | 13170 |
| `spot/web-socket-streams.md` | 22958 |

Pinning the SHA is what makes the next run's diff exact: it compares two known revisions rather than
two fetches of a moving branch.

## usd-futures — tier 2 (docs site, markdown source)

Base `https://developers.binance.com/en/docs/products/derivatives-trading-usds-futures/`, fetched by
appending `.md` to the page path. No upstream repository exists for the derivatives documentation, so
no revision can be pinned; the fetch date is the only anchor, which is precisely why the snapshot is
kept.

| file | page | bytes |
|---|---|---|
| `usd-futures/change-log.md` | `change-log` | 107349 |
| `usd-futures/general-info.md` | `general-info` | 19261 |
| `usd-futures/error-code.md` | `error-code` | 19688 |
| `usd-futures/user-data-streams.md` | `user-data-streams` | 11936 |
| `usd-futures/websocket-market-streams_Important-WebSocket-Change-Notice.md` | `websocket-market-streams/Important-WebSocket-Change-Notice` | 4303 |

### Two retrieval traps, both hit on this run

**A `200` is not proof of the page you asked for.** Unknown paths return the site's single-page-app
HTML shell with status `200` and a body of exactly 65475 bytes. Five different endpoint paths returned
byte-identical responses before this was noticed. Reject any response beginning `<!doctype html>`; the
`.md` source never does.

**The change log alone is not the documentation.** The WebSocket migration notice — the most
consequential finding of this run — is not a change-log entry. It lives on its own page, reachable
only through a link inside the change log. Follow the links out.

## Gap in this snapshot

The per-endpoint futures reference pages (exchange information, klines, new/modify/cancel order,
account, trade list) could not be located: every path tried returned the shell. This run therefore
verified the futures endpoint *inventory* against `general-info` and the change log, but not each
endpoint's parameter and response schema against its own page. Recorded as a gap rather than counted
as coverage. Finding those paths is the first task of the next run.
