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

## usd-futures request schemas — tier 1 (upstream git repository)

Repository `github.com/binance/binance-api-postman`, pinned at
**`bf7c41820ddef7684a5b861c485791ade747e8a2`**. The official Postman collections are machine-readable,
versioned and diffable — a better source for the request side than any rendered page.

| file | bytes | covers |
|---|---|---|
| `usd-futures/postman-usds-futures.json` | 260693 | 95 requests, every endpoint this module calls, with full parameter lists |
| `spot/postman-spot.json` | 238150 | the spot equivalent |

They carry **no response examples** — 0 of 95 — so they close the request side and nothing else.

## usd-futures response schemas — tier 3 (lossy)

`usd-futures/catalog-readings-tier3.md`. See its own header for what that means and what it does not.

## Rejected as a source: the Binance MCP server

`https://agent.binance.com/mcp/agentic` is not a documentation server. It is an account-connected
agent, reached by OAuth against a logged-in Binance account, and Binance's own page says plainly:
"Never paste the MCP endpoint into an AI chat and ask it to install the server." It was considered for
this gap and rejected — wrong instrument, and one whose installation the vendor explicitly warns
against doing this way.

## Gap in this snapshot

Two things remain unretrieved at a fidelity worth storing.

**The nested payloads of the user-data-stream events.** `ORDER_TRADE_UPDATE`'s order object and
`ACCOUNT_UPDATE`'s balance and position entries render from a schema source the tier-3 reading did not
reach. Our manifest holds ~20 short field names for them (`sp`, `ap`, `R`, `wb`, `cw`, `bc`, `pa`,
`ep`, `up`, …), every one still unverified against its own page.

**Everything the catalog serves, as a stored document.** Tier 3 answers a question; it does not give a
file to diff. Until the catalog can be retrieved as text, response-schema drift on futures will be
detected by asking again rather than by comparing — which is weaker, and slower to notice.

How the paths were found, for the next run: `developers.binance.com/en/docs/llms.txt` is a site index
listing every documentation page. The futures product section has exactly 13, all now snapshotted. The
catalog is not in it.
