# Catalog readings — tier 3, lossy

The USD-M futures per-endpoint reference lives in a client-rendered catalog that serves neither
markdown nor an `llms.txt` entry. These are **a model's readings of those pages**, not the pages: they
answer a specific question and cannot be diffed against a later fetch the way a stored document can.
Treated accordingly — good enough to confirm a field name we already hold, not good enough to settle a
disagreement on its own.

Read 2026-09-01 from `developers.binance.com/en/docs/catalog/core-trading-derivatives-trading-usd-s-m-futures/api/…`.

## `GET /fapi/v2/account` — `rest-api/account`

- `assets[]`: `asset`, `walletBalance`, `unrealizedProfit`, `marginBalance`, `maintMargin`,
  `initialMargin`, `positionInitialMargin`, `openOrderInitialMargin`, `crossWalletBalance`,
  `crossUnPnl`, `availableBalance`, `maxWithdrawAmount`, `marginAvailable`, `updateTime`
- `positions[]`: `symbol`, `initialMargin`, `maintMargin`, `unrealizedProfit`,
  `positionInitialMargin`, `openOrderInitialMargin`, `leverage`, `isolated`, `entryPrice`,
  `maxNotional`, `bidNotional`, `askNotional`, `positionSide`, `positionAmt`, `updateTime`

Every field our converters read is present.

## `GET /fapi/v1/order` — `rest-api/trade`

`avgPrice`, `clientOrderId`, `cumQuote`, `executedQty`, `orderId`, `origQty`, `origType`, `price`,
`reduceOnly`, `side`, `positionSide`, `status`, `stopPrice`, `closePosition`, `symbol`, `time`,
`timeInForce`, `type`, `activatePrice`, `priceRate`, `updateTime`, `workingType`, `priceProtect`,
`priceMatch`, `selfTradePreventionMode`, `goodTillDate`

Every field our get-order converter reads is present, `time` and `updateTime` included.

## `POST /fapi/v1/order` — `rest-api/trade`

`clientOrderId`, `cumQty`, `executedQty`, `orderId`, `origQty`, `price`, `reduceOnly`, `side`,
`positionSide`, `status`, `stopPrice`, `closePosition`, `symbol`, `timeInForce`, `type`, `origType`,
`updateTime`, `workingType`, `priceProtect`

**`avgPrice` is not in this list**, and our `InitOrderResponseConverter` reads it — see the run report's
unresolved section.

## `GET /fapi/v1/userTrades` — `rest-api/trade`

`buyer`, `commission`, `commissionAsset`, `id`, `maker`, `orderId`, `price`, `qty`, `quoteQty`,
`baseQty`, `marginAsset`, `realizedPnl`, `side`, `positionSide`, `symbol`, `pair`, `time`

`maker`, not `isMaker` — the spot/futures divergence the manifest records is confirmed.

## `ws-streams/~schemas`

Top-level only: `ORDER_TRADE_UPDATE` carries `e`, `E`, `T`, `o`; `ACCOUNT_UPDATE` carries `e`, `E`,
`T`, `a`. **The nested order object's fields and the balance / position entry fields were not
retrievable** — the schema page renders them from a source this reading did not reach. Our manifest's
entries for them remain unverified against their own page.
