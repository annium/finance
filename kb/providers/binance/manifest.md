---
title: Binance contract manifest
type: provider-manifest
status: living
created: 2026-09-01
checked_against: 2026-09-01
docs_revision_spot: a0057759f1cbcab812af44b75309d72866a57561
---

# Contract manifest — binance

> **Living. Changes when Binance changes.** Our own progress lives in `status.md`, deliberately apart:
> keeping them in one file would mean every reconcile run edits this document for two unrelated
> reasons, and `git log -p manifest.md` would stop answering "what did the exchange change".

Every entry is a fact owned by Binance, not by us, that this module depends on, anchored to
`file:line`. Paths are relative to the repository root; `Base`, `Spot` and `UsdFutures` abbreviate
`providers/crypto/binance/src/Annium.Finance.Providers.Crypto.Binance.<name>/`.

**`checked_against: 2026-09-01
docs_revision_spot: a0057759f1cbcab812af44b75309d72866a57561`** — derived from our code, never yet compared against Binance's
documentation. An inventory, not a baseline. `/implement-provider binance --only-layer=1` is what
changes that.

## Where the documentation comes from

Binance publishes spot and USDⓈ-M futures separately, and they must be fetched separately: a rename on
one venue and not the other produces a failure that looks venue-specific and therefore looks like ours.

| venue | source | tier | how |
|---|---|---|---|
| spot | `github.com/binance/binance-spot-api-docs` | 1 — upstream git | `curl -sSL https://raw.githubusercontent.com/binance/binance-spot-api-docs/master/<path>`; pin the commit SHA |
| usd-futures | `developers.binance.com` | 2 — site, markdown | `curl -sSL "https://developers.binance.com/en/docs/products/derivatives-trading-usds-futures/<page>.md"` |

**Appending `.md` to a developers.binance.com page returns its markdown source.** Fetching the page
itself gets an empty `202` — it is a protected single-page app — so the `.md` suffix is not a
convenience, it is the only way to retrieve that documentation faithfully.

### Page paths that work

Discovering these cost most of the first run. Spot, under
`raw.githubusercontent.com/binance/binance-spot-api-docs/<sha>/`: `CHANGELOG.md`, `enums.md`,
`errors.md`, `filters.md`, `rest-api.md`, `user-data-stream.md`, `web-socket-streams.md`.

Futures, under `developers.binance.com/en/docs/products/derivatives-trading-usds-futures/`, with `.md`
appended: `change-log`, `general-info`, `error-code`, `user-data-streams`, and
`websocket-market-streams/Important-WebSocket-Change-Notice`.

**Not found, still a gap.** The per-endpoint futures reference pages — exchange information, klines,
new / modify / cancel order, account, trade list. Every path tried returned the site's HTML shell.
Until they are located, futures request and response schemas are verified only against `general-info`
and the change log, never against their own pages. Tried and rejected:
`market-data-endpoints/…`, `trade-endpoints/…`, `account-endpoints/…`,
`user-data-streams-endpoints/…`, and `catalog/core-trading-derivatives-trading-usd-s-m-futures/api/rest-api/…`.

Known quirks, both learned the hard way:

- The derivatives change log **is truncated** when read through a summarising fetch — it returned only
  two months. Retrieve the `.md` and read it whole.
- **An unknown path returns the HTML shell with a `200`**, body exactly 65475 bytes. Five endpoint
  paths returned byte-identical responses before this was noticed. Reject anything beginning
  `<!doctype html>`, and compare sizes across a batch.
- **The change log is not the documentation.** The WebSocket migration notice — the largest finding of
  the first run — is not a change-log entry; it sits on its own page, reachable only through a link
  inside the change log. Follow the links out.
- A search result claimed a 2026-04-23 WebSocket decommissioning and the change log did not contain
  it. Held as **unresolved** rather than reported, and the separate notice settled it: the search was
  right and the change log incomplete. Two sources disagreeing stay unresolved until a third decides.

Snapshots of what was fetched live beside each run's report, so a report can be checked against the
text it was written from rather than against a page that has since moved.

## How to read an entry

Every fact carries **two independent states**. They answer different questions and are moved by
different steps, and a fact can be strong on one and worthless on the other.

**Documentation** — what Binance says: `confirmed` · `unchecked` · `undocumented` · `contested` ·
`unretrievable`.

**Verification** — what of ours would notice if it stopped being true: `pinned` (an offline test
fails) · `live` (seen in a real response, dated) · `gated` (covered only by the exchange suite, which
is skipped by default, so it proves nothing on an ordinary run) · `none` · `vacuous` (a test names it
and cannot fail — worse than `none`, because the effort looks spent).

Neither is a boolean; both take a note. Only exceptions are written out — where a category shares a
state it is stated once at its head.

### Where this manifest stands, as of 2026-09-01

Documentation is closed. Verification was censused against the test suites entry by entry, and the
result corrected two claims an earlier category-level reading of this table had got wrong — noted
below, because a summary that reads as an annotation is the failure this model exists to prevent.

| category | documentation | verification |
|---|---|---|
| 1 endpoints | `confirmed`, and this time **path by path** rather than as a set — the earlier pass checked base URLs and path shapes, which is how a wrong `v1` survived it | `pinned` for the websocket routes and both server time paths, `EndpointsTests` on each venue asserting the *composed* URL. The rest `gated` |
| 2 request parameters | `confirmed` at tier 1 from the official Postman collections | futures order shapes are **`pinned`** offline, per order type, both init and modify, with `reduceOnly` branching asserted both ways. The signing scaffolding is `gated` as a whole; `recvWindow`'s value is `none` |
| 3 response fields | spot `confirmed` at **tier 1** from `rest-api.md`; futures account / query-order / trade `confirmed` at **tier 3** (a reading, not the page); the user-data-stream nested payloads are `unretrievable` | `pinned` per converter, every one having its own test with real fixtures. Negative branches are `none`: a non-GUID cancel id, a non-`TRADING` status, a missing `SPOT` permission, an absent filter dropping the instrument |
| 4 filters | `confirmed` — every type name and field on both venues, including that spot documents **both** `MIN_NOTIONAL` and `NOTIONAL` while futures documents only `MIN_NOTIONAL` | `pinned`, including the lot-size merge arithmetic — but entirely piggybacked on the exchange-info fixture; there is no filter test of its own |
| 5 enumerations | `confirmed` on both venues against their documented lists | **not `pinned` as a category — corrected.** Only values that happen to appear in a fixture are covered. Sides and position sides are `pinned`; on the **read** side `MARKET`, `STOP_LOSS`/`STOP_MARKET`, `STOP_LOSS_LIMIT`/`STOP`, spot's `LIMIT_MAKER` fold, and every order status but `NEW` and `PARTIALLY_FILLED` are `none` |
| 6 error and status codes | `confirmed` | HTTP mapping `pinned` in all three copies. The two Binance codes are `pinned` in Spot and UsdFutures and `none` in `Base` — the drifted copy is exactly the untested one |
| 7 rate limiting | `confirmed` for the header; the decay arithmetic is `unchecked` | the mechanism is `pinned` — header casing, missing and malformed values, water mark, decay, post-dispose — and so is the **runtime `REQUEST_WEIGHT` overwrite**, now that a read-path test drives it. The production ceilings, the decay constants and the water-mark fraction remain `none` |
| 8 auth and signing | `confirmed` | `gated`, and **`vacuous`** for the percent-encoding rule. What is signed, the exclusion of `signature` itself, and the use of synced rather than local time are each `none` |
| 9 timing and lifecycle | `confirmed` | candle interval and page size `gated` against a real count. The order history window is **`pinned`**: an offline test drives twenty days through three chunks and the boundaries are observable. Trade history follows the same code and is `none` until driven. Sync cadence `none` |
| 10 hard-coded facts | mixed — `confirmed` where they mirror a documented limit, `undocumented` where they are heuristics | mostly `none`. `"BTCUSDT"` liveness and the kline page size are `gated`; the futures asset-precision heuristic is now `pinned`, which matters because it is `undocumented` — the exchange promises nothing about it, so a test is the only thing that can notice it changing |

**One route decision worth keeping visible.** The futures route lives in the *path*, never in the base.
Both call sites compose with `new Uri(base, path)`, which discards the base's path whenever the path
starts with a slash — so a route moved into the base is silently dropped and the legacy URL comes back
from configuration that reads as correct. A mutation doing exactly that is killed by
`MarketStream_ConnectsToThePublicRoute`.

**Two `vacuous` entries, and both are the same shape: the input chosen cannot exercise the property.**

1. **The signing golden value** (§8). Its query — `symbol=LTCBTC&side=BUY&…` — contains no character
   requiring percent-encoding, so it passes identically whether or not the implementation encodes
   before signing, which Binance has required since 2026-01-15.
2. ~~The history paging windows~~ — **closed**. The gated fixture still asks for one day and still
   proves nothing, but an offline test now drives twenty days through three windows and asserts the
   boundaries, so the fact is `pinned` regardless of what the gated one does.

**Five components have no test file at all:** `WebSocketService`, `ListenKeyResolver`,
`HttpRequestSignatureExtensions`, `HttpRequestLogExtensions`, and the filter converters. The first two
carry the connection lifecycle for every stream this module runs.

**[DEFECT, upstream] An exchange error is discarded whenever the success type is a collection.**
`Annium.Net.Http`'s `AsResponseExtensions` parses the success type first; when that throws — as it does
for any error body against a collection type — control leaves for the catch and the branch that would
have read Binance's `{code, msg}` never runs. So `-2015 Invalid API-key` reaches the caller as
`ParseError`. Every read endpoint returning a list is affected: open orders, all orders, user trades,
klines. The account context is the contrast — an object success type parses into a defaulted instance
rather than throwing, so its code is read. Both behaviours are pinned in
`UserProviderReadPathTests`; the fix is in another repository.

**One lesson from the first live run, kept where the next reader meets it.** A `[DIVERGES]` marker is a
note that something looks odd. It is not a check, and after a while it reads like one. Spot's server
time path carried that marker for the whole contract pass and was simply wrong; the run that found it
was the first thing to actually call the endpoint. Markers record; only a comparison confirms.

**Where the exposure is.** The `none` bucket is the largest by a wide margin, and its centre of mass is
not where anyone would guess: not the exotic paths, but the constants. Production rate-limit ceilings,
`recvWindow`, the water mark — the values most likely to be copied wrong and least likely to be
noticed. The precision heuristic has since been pinned; the rest have not.

Structural markers stay separate from both axes: **[DIVERGES]** between spot and futures,
**[DUPLICATED]** across files, **[DEAD]** for code unreachable in production, **[DRIFT]** where what we
have no longer matches what Binance documents.

# Contract manifest — binance

> **Living. Changes when Binance changes.** Our own progress lives in `status.md`, deliberately apart:
> keeping them in one file would mean every reconcile run edits this document for two unrelated
> reasons, and `git log -p manifest.md` would stop answering "what did the exchange change".

Every entry is a fact owned by Binance, not by us, that this module depends on, anchored to
`file:line`. Paths are relative to the repository root; `Base`, `Spot` and `UsdFutures` abbreviate
`providers/crypto/binance/src/Annium.Finance.Providers.Crypto.Binance.<name>/`.

**`checked_against: 2026-09-01
docs_revision_spot: a0057759f1cbcab812af44b75309d72866a57561`** — derived from our code, never yet compared against Binance's
documentation. An inventory, not a baseline. `/implement-provider binance --only-layer=1` is what
changes that.

## Where the documentation comes from

Binance publishes spot and USDⓈ-M futures separately, and they must be fetched separately: a rename on
one venue and not the other produces a failure that looks venue-specific and therefore looks like ours.

| venue | source | tier | how |
|---|---|---|---|
| spot | `github.com/binance/binance-spot-api-docs` | 1 — upstream git | `curl -sSL https://raw.githubusercontent.com/binance/binance-spot-api-docs/master/<path>`; pin the commit SHA |
| usd-futures | `developers.binance.com` | 2 — site, markdown | `curl -sSL "https://developers.binance.com/en/docs/products/derivatives-trading-usds-futures/<page>.md"` |

**Appending `.md` to a developers.binance.com page returns its markdown source.** Fetching the page
itself gets an empty `202` — it is a protected single-page app — so the `.md` suffix is not a
convenience, it is the only way to retrieve that documentation faithfully.

### Page paths that work

Discovering these cost most of the first run. Spot, under
`raw.githubusercontent.com/binance/binance-spot-api-docs/<sha>/`: `CHANGELOG.md`, `enums.md`,
`errors.md`, `filters.md`, `rest-api.md`, `user-data-stream.md`, `web-socket-streams.md`.

Futures, under `developers.binance.com/en/docs/products/derivatives-trading-usds-futures/`, with `.md`
appended: `change-log`, `general-info`, `error-code`, `user-data-streams`, and
`websocket-market-streams/Important-WebSocket-Change-Notice`.

**Not found, still a gap.** The per-endpoint futures reference pages — exchange information, klines,
new / modify / cancel order, account, trade list. Every path tried returned the site's HTML shell.
Until they are located, futures request and response schemas are verified only against `general-info`
and the change log, never against their own pages. Tried and rejected:
`market-data-endpoints/…`, `trade-endpoints/…`, `account-endpoints/…`,
`user-data-streams-endpoints/…`, and `catalog/core-trading-derivatives-trading-usd-s-m-futures/api/rest-api/…`.

Known quirks, both learned the hard way:

- The derivatives change log **is truncated** when read through a summarising fetch — it returned only
  two months. Retrieve the `.md` and read it whole.
- **An unknown path returns the HTML shell with a `200`**, body exactly 65475 bytes. Five endpoint
  paths returned byte-identical responses before this was noticed. Reject anything beginning
  `<!doctype html>`, and compare sizes across a batch.
- **The change log is not the documentation.** The WebSocket migration notice — the largest finding of
  the first run — is not a change-log entry; it sits on its own page, reachable only through a link
  inside the change log. Follow the links out.
- A search result claimed a 2026-04-23 WebSocket decommissioning and the change log did not contain
  it. Held as **unresolved** rather than reported, and the separate notice settled it: the search was
  right and the change log incomplete. Two sources disagreeing stay unresolved until a third decides.

Snapshots of what was fetched live beside each run's report, so a report can be checked against the
text it was written from rather than against a page that has since moved.

## How to read the markers

Only exceptions are marked. A fact confirmed at the last check needs nothing — `checked_against`
covers it. The reader needs the list of what cannot be trusted, not a list of everything.

- **[UNVERIFIED]** — derived from our code, never checked against Binance's documentation. Everything
  below is currently this, by construction.
- **[UNDOCUMENTED]** — we depend on it and the documentation does not state it. Inferred from observed
  behaviour, so it changes with no changelog entry and **no drift check will catch it in advance**.
  None identified yet: separating these from the merely unverified is what the first documentation
  pass does.
- **[LIVE]** — confirmed by an actual exchange response, with the date. Upgraded only by layer 6.
- **[DIVERGES]** — spot and futures assume different things here.
- **[DUPLICATED]** — encoded in more than one place, so a change must be made more than once.
- **[DEAD]** — encoded but unreachable from production. Recorded anyway: reviving the path revives the
  assumption.

---

## 1. Endpoints

### Base URLs

| Fact | Where |
|---|---|
| Spot HTTP `https://api.binance.com` | `Spot/Internal/Shared/Endpoints.cs:11` |
| Spot WS `wss://stream.binance.com` | `Spot/Internal/Shared/Endpoints.cs:14` |
| Futures HTTP `https://fapi.binance.com` | `UsdFutures/Internal/Shared/Endpoints.cs:11` |
| Futures WS `wss://fstream.binance.com`, unrouted, with the route in the path: `/public/stream` for market, `/private/ws/` for user data. `/market` carries the regular feeds and this provider subscribes to none of them | `UsdFutures/Internal/Shared/Endpoints.cs:26,29,32` |

Sandbox base URLs are gone: all testing is against the live exchange, so the environment concept was
removed from the code entirely rather than kept and corrected. The futures sandbox had moved to
`demo-fapi` / `demo-fstream` and the spot sandbox websocket host had never been right — both are
recorded here only so a future reader knows the omission is deliberate.

### REST paths

| Method and path | Venue | Where |
|---|---|---|
| `GET api/v3/exchangeInfo` | spot | `Spot/Internal/Market/MarketProvider.cs:48` |
| `GET api/v3/klines` | spot | `Spot/Internal/Market/MarketProvider.cs:91` |
| `GET /api/v3/time` — **[DRIFT, fixed 2026-09-02]** we called `/api/v1/time` and the first live read run failed on it. The manifest had recorded the version oddity as a divergence and never checked it against the documented endpoint list; a curiosity stood in for a verified fact | spot | `Spot/Internal/Shared/Endpoints.cs:22` |
| `GET fapi/v1/exchangeInfo` | futures | `UsdFutures/Internal/Market/MarketProvider.cs:51` |
| `GET fapi/v1/klines` | futures | `UsdFutures/Internal/Market/MarketProvider.cs:100` |
| `GET /fapi/v1/time` **[DIVERGES]** — `v1` here is correct, unlike spot | futures | `UsdFutures/Internal/Shared/Endpoints.cs:35` |
| `GET /fapi/v2/account` — note `v2` | futures | `UsdFutures/Internal/User/UserProvider.cs:68` |
| `GET /fapi/v1/openOrders` | futures | `UsdFutures/Internal/User/UserProvider.cs:103` |
| `GET /fapi/v1/allOrders` | futures | `UsdFutures/Internal/User/UserProvider.cs:163,207,245` |
| `GET /fapi/v1/userTrades` | futures | `UsdFutures/Internal/User/UserProvider.cs:289,332,370` |
| `POST /fapi/v1/leverage` | futures | `UsdFutures/Internal/User/UserConnector.cs:180` |
| `POST /fapi/v1/order` | futures | `UsdFutures/Internal/User/UserConnector.cs:216` |
| `PUT /fapi/v1/order` (modify) | futures | `UsdFutures/Internal/User/UserConnector.cs:272` |
| `DELETE /fapi/v1/order` | futures | `UsdFutures/Internal/User/UserConnector.cs:307` |
| `DELETE /fapi/v1/allOpenOrders` | futures | `UsdFutures/Internal/User/UserConnector.cs:342` |
| `POST /fapi/v1/listenKey` | futures | `UsdFutures/Internal/User/UserConnectorFactory.cs:56` |
| Spot cancel-replace endpoint **[DEAD]** — parameters built, path never issued | spot | `Spot/Internal/User/Services/QueryProcessor.cs:68-111` |

### WebSocket

| Fact | Where |
|---|---|
| Combined stream path — `/stream` on spot, `/public/stream` on futures **[DIVERGES]** | `Spot/Internal/Market/Profiles/MarketConfigProfile.cs:29`, `UsdFutures/.../MarketConfigProfile.cs:38` |
| Book ticker topic `{symbol}@bookTicker`, symbol lowercased | `Base/Internal/Market/Services/BookTickerService.cs:75` |
| User stream URI is `{WsApi}{ListenKeyUriPath}{listenKey}` — `/ws/` on spot, `/private/ws/` on futures **[DIVERGES]** | `Base/Internal/User/Services/UserStream.cs:112`; path from `Spot/.../UserConfigProfile.cs:37`, `UsdFutures/.../UserConfigProfile.cs:47` |

---

## 2. Request parameters

### Signed-request scaffolding — `Base/Shared/HttpExtensions/HttpRequestSignatureExtensions.cs`

| Fact | Line |
|---|---|
| API key header is `x-mbx-apikey` | 19-20 |
| `recvWindow` sent on every signed request, hard-coded `30_000` | 25 |
| `timestamp` and `signature` appended as query params | 27-44 |

### Klines

`symbol`, `interval` (always `"1m"`), `limit`, `startTime` — `Spot/Internal/Market/MarketProvider.cs:92-95`,
`UsdFutures/Internal/Market/MarketProvider.cs:101-104`.

### Futures orders — `UsdFutures/Internal/User/Services/QueryProcessor.cs`

| Fact | Line |
|---|---|
| Always sent: `newClientOrderId`, `symbol`, `side`, `positionSide`, `type`, `newOrderRespType="RESULT"` | 32-37 |
| Limit adds `timeInForce="GTC"`, `quantity`, `price` | 42-44 |
| Market adds `quantity` | 48 |
| Stop/take-profit market add `quantity`, `stopPrice` | 52-59 |
| Stop/take-profit limit add `timeInForce="GTC"`, `quantity`, `price`, `stopPrice` | 62-74 |
| `reduceOnly="true"` sent **only** in one-way mode — Binance rejects it alongside an explicit `positionSide` in hedge mode | 79-83 |
| Modify supports `Limit` only; sends `origClientOrderId`, `symbol`, `side`, `quantity`, `price`. Binance also accepts optional `priceMatch` and `modifyId`, which we do not send | 96-107 |
| Cancel sends `orderId` and/or `origClientOrderId` + `newClientOrderId`, `symbol` | 119-137 |
| `leverage` floored to int32 | `UsdFutures/Internal/User/UserConnector.cs:181-182` |

### Spot orders **[DEAD]** — `Spot/Internal/User/Services/QueryProcessor.cs`

Same base set minus `positionSide`/`reduceOnly` (24-28); modify via cancel-replace with
`cancelReplaceMode="STOP_ON_FAILURE"`, `cancelOrigClientOrderId`, `newClientOrderId`,
`timeInForce="GTC"` (72-79). Never invoked — see §9.

Its **response** shape is encoded too: top-level `code` / `msg` / `data`, with `data.cancelResponse`
and `data.newOrderResponse` nested inside, and the cancel leg's error preferred over the init leg's
when only one failed — `Spot/.../ModifyOrderFailureResponseConverter.cs:60-129`,
`ModifyOrderSuccessResponseConverter.cs:49-50`. Also `[DEAD]`.

---

## 3. Response fields

### Exchange info

- Rate limits: entry with `rateLimitType`/`limit`; only `"REQUEST_WEIGHT"` is read, and its window is
  **assumed to already be one minute** — `Base/Market/Contracts/Converters/RateLimitsConverter.cs:37-44`, field names read at `:68,71`
  **[UNVERIFIED]**
- Spot instrument: `symbol`, `status` (must be `"TRADING"`), `baseAsset`, `baseAssetPrecision`,
  `quoteAsset`, `quoteAssetPrecision`, `isSpotTradingAllowed`, `filters`, `permissions[]` /
  `permissionSets[][]` must contain `"SPOT"` — `Spot/Internal/Market/Contracts/Converters/InstrumentConverter.cs:50-121`
- Futures instrument: `symbol`, `contractType` (must be `"PERPETUAL"`; delivery contracts dropped),
  `status`, `baseAsset`, `baseAssetPrecision`, `quoteAsset`, **`quotePrecision`** — **[DIVERGES]**, spot
  spells the same idea `quoteAssetPrecision` — `UsdFutures/.../InstrumentConverter.cs:49-107`
- Futures assets: `assets[]` with `asset`, `marginAvailable` — `UsdFutures/.../AssetConverter.cs:28-62`

### Exchange information envelope

| Fact | Where |
|---|---|
| Top level carries `rateLimits` and `symbols` (spot) | `Spot/.../ExchangeInfoConverter.cs:53,56` |
| Top level carries `rateLimits`, `assets` and `symbols` (futures) **[DIVERGES]** | `UsdFutures/.../ExchangeInfoConverter.cs:58,61,64` |

### Market data

| Fact | Where |
|---|---|
| Book ticker `s`, `b`, `a`; a record with both prices zero is dropped | `Base/Market/Contracts/Converters/InstrumentTickerConverter.cs:52-64` |
| Kline is a **positional array**: 0 open time, 1 open, 2 high, 3 low, 4 close, 5 volume; 6+ ignored | `Base/Market/Contracts/Converters/CandleConverter.cs:48-71` |
| Server time `{"serverTime": long}` | `Base/Shared/Contracts/Converters/ServerTimeConverter.cs:42-43` |
| Error envelope `{"code": long, "msg": string}` | `Base/Shared/Contracts/Converters/OperationResultConverter.cs:43-47` |
| WS control ack `{"id": long, "result": …}` | `Base/Shared/Contracts/Converters/CommandResultConverter.cs:44-48` |
| WS control **request** `{"id": auto-increment, "method": "SUBSCRIBE"\|"UNSUBSCRIBE", "params": [topics]}` | `Base/Internal/Market/Services/WebSocketService.cs:100,120,179-195` |
| Combined-stream envelope `{"stream": string, "data": {…}}` | `Base/Shared/Contracts/Converters/StreamDataConverter.cs:45-49` |
| Listen key `{"listenKey": string}` | `Base/User/Contracts/Converters/ListenKeyResponseConverter.cs:37-39` |

### Account

- Spot: `balances[]` with `asset`, `free`, `locked` — `Spot/.../GetAccountResponseConverter.cs:51-56`,
  `GetAccountResponseBalanceConverter.cs:45-57`
- Futures `/fapi/v2/account`: `assets[]` with `asset`, `marginBalance`, `maxWithdrawAmount`,
  `initialMargin`, `maintMargin`, `updateTime`; `positions[]` with `symbol`, `positionSide`, `isolated`,
  `leverage`, `positionAmt`, `entryPrice`, `unrealizedProfit`, `updateTime` —
  `UsdFutures/.../GetAccountResponseBalanceConverter.cs:65-82`, `GetAccountResponsePositionConverter.cs:71-94`
- **[UNVERIFIED]** A one-way account is assumed to report one `positions[]` row per symbol regardless of
  whether a position is open, always with `positionSide=BOTH`. The test fixture's position-mode
  precondition depends on this — `providers/base/tests/Annium.Finance.Providers.Tests.Lib/User/UserConnectorTestBase.cs`

### Orders and trades

| Fact | Where |
|---|---|
| Spot order: `orderId`, `clientOrderId`, `symbol`, `type`, `side`, `origQty`, `price`, `stopPrice`, `status`, `executedQty`, `cummulativeQuoteQty` (executed price **derived** as sum ÷ qty), `time`, `updateTime` | `Spot/.../GetOrderResponseConverter.cs:79-121` |
| Futures order: same core plus `positionSide`, `reduceOnly`, and `avgPrice` used **directly** — **[DIVERGES]** | `UsdFutures/.../GetOrderResponseConverter.cs:87-135` |
| Spot init-order uses `workingTime` for created and `transactTime` for updated — **[DIVERGES]** from its own get-order, which uses `time`/`updateTime` | `Spot/.../InitOrderResponseConverter.cs:115-120` |
| Futures init-order has **no creation timestamp**; `updateTime` serves as both | `UsdFutures/.../InitOrderResponseConverter.cs:126-128` |
| **[CONTESTED]** the same converter reads `avgPrice`; the catalog lists that field on the query-order response and **not** on the new-order response. If the listing is right, a placed order returns an executed price of zero. The reading is tier 3 and cannot settle it — the first live order will | `UsdFutures/.../InitOrderResponseConverter.cs:123` |
| Trade: `id`, `orderId`, `symbol`, `qty`, `price`, `commission`, `commissionAsset`, `time` | both `GetTradeResponseConverter.cs` |
| Maker flag is `isMaker` on spot, `maker` on futures — **[DIVERGES]** | `Spot/.../GetTradeResponseConverter.cs:91`, `UsdFutures/.../GetTradeResponseConverter.cs:97` |
| Cancel response `clientOrderId` is parsed **as a GUID**; a non-GUID id makes the whole response read as missing | `Spot/.../CancelOrderResponseConverter.cs:50-55`, `UsdFutures/.../CancelOrderResponseConverter.cs:54-59` |
| Leverage response `{"leverage": decimal-string}` | `UsdFutures/.../LeverageResponseConverter.cs:41-43` |

### User data stream events

| Event | Fields | Where |
|---|---|---|
| Spot `executionReport` **[DEAD]** | `e`,`s`,`t`,`i`,`c`,`o`,`S`,`q`,`p`,`P`,`X`,`z`,`Z`,`l`,`L`,`n`,`N`,`m`,`O`,`T` | `Spot/.../OrderUpdateEventConverter.cs:99-163` |
| Spot `outboundAccountPosition` **[DEAD]** | `e`,`u`,`B[]` with `a`,`f`,`l` | `Spot/.../AccountUpdateEventConverter.cs:63-84` |
| Futures `ORDER_TRADE_UPDATE` | top-level `e`, nested `o` with `s`,`t`,`i`,`c`,`o`,`S`,`q`,`p`,`sp`,`R`,`X`,`z`,`ap`,`l`,`L`,`n`,`N`,`m`,`T`. Trigger price is `sp` where spot uses `P`; average price is `ap` where spot derives it. `createdAt` synthesized from `transactionTime` only when status is `New`, else `0` | `UsdFutures/.../OrderUpdateEventConverter.cs:83,104-185` |
| Futures `ACCOUNT_CONFIG_UPDATE` | `e`,`T`,`ai` (presence ⇒ multi-assets change), `ac` (presence ⇒ leverage change), `j`,`s`,`l` | `UsdFutures/.../AccountConfigUpdateEventConverter.cs:73-98` |
| Futures `ACCOUNT_UPDATE` | `e`,`T`,`a`, `B[]` with `a`,`wb`,`cw`,`bc`, `P[]` with `s`,`ps`,`mt`,`iw`,`pa`,`ep`,`up` | `UsdFutures/.../BalanceAndPositionUpdateEventConverter.cs:67-89` |

---

## 4. Exchange filters

| Filter | Spot | Futures |
|---|---|---|
| `PRICE_FILTER` | `minPrice`, `maxPrice`, `tickSize` | same |
| `LOT_SIZE` + `MARKET_LOT_SIZE` | merged as max-of-mins, min-of-maxes, max-of-steps **[DUPLICATED]** `Spot/.../InstrumentFiltersConverter.cs:68-72` | identical logic `UsdFutures/.../InstrumentFiltersConverter.cs:70-74` |
| Notional **[DIVERGES]** | type `"NOTIONAL"`, fields `minNotional` and `maxNotional` — `Spot/.../InstrumentFiltersConverter.cs:96-98,137-141` | type `"MIN_NOTIONAL"`, single field `"notional"`, max **hard-coded** to `decimal.MaxValue` — `UsdFutures/.../InstrumentFiltersConverter.cs:98-100,139-140` |
| `MAX_NUM_ORDERS` **[DIVERGES]** | field `maxNumOrders` | field `limit` |

**Absence behaviour:** if the price, lot-size, notional or max-orders filter is missing, the filters
object reads as `null` and `InstrumentConverter` drops **the entire instrument**. An unenforced bound
therefore does not arrive as a zero field — the symbol simply never appears. Both converters, end of
array.

---

## 5. Enumerations

**Order side** — `BUY` / `SELL`, both venues. `Spot/.../OrderSides.cs:19`, `UsdFutures/.../OrderSides.cs:21`

**Order status** — `NEW`, `PARTIALLY_FILLED`, `FILLED`, `CANCELED`, `REJECTED`, `EXPIRED`. Spot also
folds `PENDING_CANCEL` → `Canceled` and `EXPIRED_IN_MATCH` → `Rejected`
(`Spot/.../OrderStatuses.cs:38,41`); futures folds only `EXPIRED_IN_MATCH` **[DIVERGES]**
(`UsdFutures/.../OrderStatuses.cs:40`) — confirmed against the documented futures status list, which has no
`PENDING_CANCEL`.

**[DRIFT] Spot documents `PENDING_NEW` and we do not map it** — an order in an order list waits in that
state until its working order fills. Our lookup would find nothing for it. `[DEAD]` in practice, since
the spot user path throws before any of this runs, but it is a hole in the mapping rather than a
deliberate omission. Binance also notes `PENDING_CANCEL` is "currently unused", so our folding of it
costs nothing and proves nothing.

**Symbol status is not a two-value question.** Spot documents `TRADING`, `END_OF_DAY`, `HALT`, `BREAK`
and `CANCEL_ONLY`; futures documents `PENDING_TRADING`, `TRADING`, `PRE_DELIVERING`, `DELIVERING`,
`DELIVERED`, `PRE_SETTLE`, `SETTLING`, `CLOSE`, `TRADING_HALT` and `TRADING_CANCEL_ONLY`. Our
converters admit `TRADING` alone, so **every other state drops the instrument entirely** — a halted
symbol becomes indistinguishable from one the exchange never listed. Defensible for opening a
position; wrong for a connector holding one, which will see the instrument vanish rather than learn it
was halted.

**Order type — [DIVERGES], entirely different naming schemes:**

| Domain | Spot | Futures |
|---|---|---|
| Limit | `LIMIT` | `LIMIT` |
| Market | `MARKET` | `MARKET` |
| StopLossMarket | `STOP_LOSS` | `STOP_MARKET` |
| TakeProfitMarket | `TAKE_PROFIT` | `TAKE_PROFIT_MARKET` |
| StopLossLimit | `STOP_LOSS_LIMIT` | `STOP` |
| TakeProfitLimit | `TAKE_PROFIT_LIMIT` | `TAKE_PROFIT` |

Spot `Spot/.../OrderTypes.cs:23-40` also folds `LIMIT_MAKER` → `Limit` on read; futures
`UsdFutures/.../OrderTypes.cs:22-41` folds `TRAILING_STOP_MARKET` → `StopLossMarket`.

**Margin type** (futures) — `"isolated"` / `"cross"`, lowercase, `UsdFutures/.../MarginTypes.cs:24-25`.
Note the same concept arrives as a **boolean** `isolated` over REST and as the string `mt` over the
stream — `GetAccountResponsePositionConverter.cs:78` vs `BalanceAndPositionUpdateEventPositionConverter.cs:77`.

**Position side** (futures only) — `BOTH` / `LONG` / `SHORT`, `UsdFutures/.../OrientationRanges.cs:24-26`.
Spot has no concept of it and hard-codes `Both` throughout its converters.

---

## 6. Error and status codes

**HTTP** — `418` (a literal cast, no named enum member) and `429` both map to `TooManyRequests`; `400`
to `BadRequest`; `401`/`403` to `Forbidden` and `404` to `NotFound` on user endpoints; everything else
to `UnknownError`. **[DUPLICATED]** across `Base`, `Spot` and `UsdFutures` result extensions, e.g.
`Base/Internal/Market/HttpExtensions/HttpRequestMarketResultExtensions.cs:64`.

**Binance codes**

| Code | Meaning | Mapped to |
|---|---|---|
| `-2018` | `BALANCE_NOT_SUFFICIENT` | `InsufficientBalance` |
| `-2019` | `MARGIN_NOT_SUFFICIENT` | `InsufficientBalance` |
| any other negative | — | `BadRequest` |

**[DUPLICATED] and already drifted:** the two special cases appear in
`Spot/Internal/User/HttpExtensions/HttpRequestUserResultExtensions.cs:80-81` and
`UsdFutures/.../HttpRequestUserResultExtensions.cs:93-94`, but **not** in
`Base/Internal/User/HttpExtensions/HttpRequestUserResultExtensions.cs:74-81`, which has only the
generic fallback. A new Binance code needs adding in two places, and the third copy is already behind.

**Local, not Binance:** `NetworkError=1`, `Aborted=2`, `ParseError=3` —
`Base/Shared/Contracts/Domain/OperationResult.cs:9-15`.

---

## 7. Rate limiting

| Fact | Where |
|---|---|
| Weight header `x-mbx-used-weight-1m`, matched case-insensitively | `Base/Shared/HttpExtensions/HttpRequestRateExtensions.cs:43` |
| A missing or unparseable header leaves the weight unchanged; the response is still returned | same, 48-64 |
| Initial ceilings: spot `6000`/min, futures `2400`/min | `Spot/ProviderRegistrationContextExtensions.cs:106`, `UsdFutures/...:118` |
| Decay `300` every `3000`ms on **both** — i.e. 6000/min, which does not match the futures ceiling **[UNVERIFIED]** | same lines |
| Binance also returns an `x-mbx-order-*` family of order-count limit headers; the code knows to mask both prefixes in logs but reads neither | `Base/Shared/HttpExtensions/HttpRequestLogExtensions.cs:10` |
| Ceiling is overwritten at runtime from exchange-info's `REQUEST_WEIGHT` | `Spot/Internal/Market/MarketProvider.cs:63-65`, `UsdFutures/...:69-71` |
| Local gate at 80% of the ceiling, before the request is sent | `providers/base/src/Annium.Finance.Providers.Core/Internal/Shared/RateLimits/RateLimiter.cs:17,88` |
| A locally-gated request is synthesized as `429` | `Base/Shared/HttpExtensions/HttpRequestRateExtensions.cs:26-39` |

Nothing reads `Retry-After`, and 418 is not distinguished from 429 — see the queued rate-limit work in
the campaign report.

---

## 8. Auth and signing

| Fact | Where |
|---|---|
| HMAC-SHA256 over the query string, hex, lowercase. Since 2026-01-15 the payload must be **percent-encoded before signing** or the request is rejected `-1022`. We sign `Uri.Query`, which is already encoded — compliant, but by construction rather than by intent, and **nothing pins it**: the golden-value test's query contains no character needing encoding | `Base/Internal/User/Services/SignatureService.cs:49-54` |
| Signed content is the full query string built so far, excluding `signature` | `Base/Shared/HttpExtensions/HttpRequestSignatureExtensions.cs:31-44` |
| `timestamp` is the **synced server time**, not the local clock | same, 35 |
| Listen key fetch and keep-alive both issue **`POST`** — correct: a `POST` on an account with an active key returns it and extends validity 60 minutes. The class doc comment claiming "periodic PUT" is what is wrong | `Base/Internal/User/Services/ListenKeyResolver.cs:130` |

**Settled 2026-09-01.** The futures documentation states that a `POST` on an account with an active
`listenKey` returns that key and extends its validity for 60 minutes. Our POST-only resolver is
correct; the class doc comment promising "periodic PUT keep-alive confirmations" is the error, and a
prior review reporting this as a defect was wrong. Still open for **spot**, where `PUT` is the
keep-alive and `POST` may mint a new key — relevant only if the spot user path is ever revived.

Keep-alive cadence is `60_000`ms with a `5_000`ms fetch retry — `Spot/ProviderConfiguration.cs:11`,
`UsdFutures/ProviderConfiguration.cs:16`.

---

## 9. Timing and lifecycle

| Fact | Where |
|---|---|
| Candle interval is always `"1m"`; page size `1000` | both `MarketProvider.cs` |
| Order and trade history paged at `1000`, in 7-day windows. Reach is capped by Binance at **3 months** for `userTrades` (reduced from 6 on 2026-08-26); `allOrders`' `symbol` became optional 2026-08-25, though we always send it | `UsdFutures/Internal/User/UserProvider.cs:44-53` |
| Server time synced at `2_000`ms until first success, `5_000`ms after | `Spot/ProviderConfiguration.cs:14`, `UsdFutures/...:19` |

**[DEAD] — the whole spot user path.** `Spot/Internal/User/UserConnectorFactory.cs:14-31` builds a
connector with no listen-key resolver and no user stream, and `Spot/Internal/User/UserConnector.cs:45-80`
throws `NotImplementedException` for trading, leverage and stream updates. Every spot assumption above
marked **[DEAD]** — the cancel-replace parameters, the `executionReport` and `outboundAccountPosition`
field maps — is unreachable from production today. Recorded because reviving the path revives them.

---

## 10. Hard-coded exchange facts

| Value | Meaning | Where |
|---|---|---|
| `recvWindow = 30_000` | request validity window | `Base/Shared/HttpExtensions/HttpRequestSignatureExtensions.cs:25` |
| `newOrderRespType = "RESULT"` | response verbosity, every order | both `QueryProcessor.cs` |
| `timeInForce = "GTC"` | never `IOC` or `FOK` | both `QueryProcessor.cs` |
| ceilings `6000` / `2400`, decay `300`/`3000ms` | §7 | — |
| water mark `0.8f` | local gate fraction | `RateLimiter.cs:17` |
| page size `1000` | klines, orders, trades | §9 |
| `"BTCUSDT"` | assumed live and tradable on both venues | test fixtures, `Spot.Tests/.../MarketConnectorTests.cs:24` and the futures twin |
| precision `8`, or `2` when the code contains `"USD"` | fallback for a futures asset not seen as an instrument resource — a heuristic standing in for real precision data | `UsdFutures/Internal/Market/MarketProvider.cs:66-67` |
| settlement currency **is** the quote asset | passed as both `Quote` and `Currency` | `Spot/.../InstrumentConverter.cs:66-67`, `UsdFutures/...:60-61` |

---

## Assumptions carried only by tests

- `"BTCUSDT"` being live and tradable is asserted only by fixtures that talk to the real exchange.
- The HMAC algorithm is pinned by a golden value in `test.env` (`TEST_EXPECTED_SIGNATURE`), not by any
  production assertion.
- The `-2018` / `-2019` mappings have no test in `Base.Tests`, which is why the drifted `Base` copy
  (§6) goes unnoticed.

## Highest drift risk, ranked

1. **Order type wire strings** — six values, two venues, entirely different schemes (§5).
2. **Notional filter** — different type name, different field name, synthesized maximum (§4).
3. **Error codes** — three copies, one already out of sync (§6).
4. **Trade maker flag** — one letter apart between venues (§3).
5. **Kline positional indices** — a positional array has no names to protect it (§3).
6. **Listen-key method** — the one place where the code and its own documentation disagree (§8).
