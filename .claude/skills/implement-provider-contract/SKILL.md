---
name: implement-provider-contract
description: Reconcile an exchange provider's wire contract — derive every API fact the module depends on, verify the manifest still matches the code, diff it against the exchange's current documentation, and report what drifted and what it costs us. This is the drift check. Use as layer 1 of implement-provider, or standalone when the user says "сверь контракт", "проверь дрейф API", "check the provider contract", or when an exchange test fails for a reason that might not be our defect.
user-invocable: true
---

# Implement Provider — Contract

Produce and keep current the **wire contract** for one exchange provider: every fact about someone
else's API that this module depends on. The contract is the target every other layer is measured
against, so this layer runs first and re-converges first.

Assessing the contract against the exchange's documentation **is** the drift check. There is no
separate skill for it, and there should not be: a drift report that is not written back into a target
is a document nobody reads twice.

## Why this exists

This module encodes several hundred facts it does not own — endpoint paths, parameter names, JSON
property names, filter type strings, status spellings, numeric error codes, the positional layout of a
kline. None of them announce themselves when they change.

When the exchange moves one, a test fails, and the failure looks exactly like our own defect. The
natural response — read our code, find nothing wrong, read it again — costs hours and sometimes ends
in "fixing" correct code. This check exists so the first question after such a failure is answerable
in minutes: **did the thing we depend on change?**

Run it *before* validating against the exchange, not after things start failing.

## Safety

- **NEVER set `FINANCE_EXCHANGE_TESTS`.** Nothing here runs a test or calls the exchange. This layer
  reads code and reads documentation.
- `test.env` holds real credentials. Never read them for their values, never print them.

## The documents

```
kb/providers/<provider>/
  manifest.md                     the contract — this layer owns it
  status.md                       convergence and history — this layer writes its own rows
  <YYYY.MM>/<YYYY.MM.DD>-contract.md      the run's report, immutable
  <YYYY.MM>/<YYYY.MM.DD>-docs/            the documentation this run read
```

One manifest per exchange, with a shared section and one per market type, because the divergences
between market types are the drift-prone part and are only visible when both halves sit on the same
page.

## Workflow

### Phase 0 — establish the tree

Clean and on `main` level with `origin/main`, or on `feature/<provider>`. Anything else stops the run;
report the actual state and ask. Never stash, never force, never hard-reset.

### Phase 1 — derive, or verify, the manifest against the code

**On a first run** there is no manifest. Derive the manifest from the code: sweep the provider's tree and
record every fact that belongs to the exchange, anchored to `file:line`, in the categories below. Mark
everything `[UNVERIFIED]` — derived from our code, not yet checked against anything. Set
`checked_against: never`. This is an inventory, not yet a baseline.

**On every later run**, verify the manifest still describes *this* code before comparing it to anything
external:

- **Every anchor resolves to what it claims.** Line numbers move with every edit, and a manifest
  pointing at the wrong line is worse than none — it will be trusted. Fix the ones that moved.
- **Every assumption in the code has an entry.** Sweep for new ones. This gap is the blind spot the
  document exists to close, and it opens quietly: a converter gains a field, nobody updates the manifest,
  and that field is now outside every future drift check.
- **Every entry still corresponds to live code.** An entry for something deleted becomes `[DEAD]` or
  goes.

Delegate this to a fresh subagent given the manifest and the tree. A reviewer who wrote the manifest will
read what it meant to say.

### Phase 2 — snapshot the documentation

Do not read the documentation live and compare it against memory. **Fetch it, store it, and diff it
against the previous run's snapshot.** Memory of a vendor's API is exactly the thing that drifts, so a
check that relies on it is checking the wrong artefact.

Retrieve in tiers, best first, and record which tier each file came from:

| tier | source | how |
|---|---|---|
| 1 | the vendor's own docs repository | `curl -sSL https://raw.githubusercontent.com/<org>/<repo>/<ref>/<path>` — and pin the commit SHA |
| 2 | a docs site that serves markdown | try `curl -sSL "<page-url>.md"` — many documentation sites serve their source this way even when the page itself is a protected single-page app that returns an empty `202` to a plain fetch |
| 3 | `WebFetch`, only where neither works | **lossy**: it returns a model's reading of the page, not the page. Mark every file so retrieved, and say so in the report |

**Do not build the process around tier 1.** Most providers publish no repository at all, and the check
must be as good without one — the snapshot in our repository is what supplies the history, and it does
that whichever tier filled it.

Where a vendor publishes separately per market type, fetch each. A rename on one venue and not the
other produces a failure that looks venue-specific, and therefore looks like ours.

Record the page paths that worked, and the ones that did not, in the manifest's documentation section.
Locating a vendor's real page paths is a substantial part of a first run's cost and there is no reason
to pay it twice.

Scope the snapshot to what the manifest references, plus changelogs. A full documentation set is mostly
about things we do not use, and volume nobody diffs is volume that hides the diff.

#### Verify what you actually fetched

**A `200` with a body is not proof you got the page you asked for.** A documentation site backed by a
single-page app answers an unknown path with its HTML shell, status `200`, and a plausible-looking
body. On the first run against Binance's futures docs, five different endpoint paths returned
**byte-identical** responses of exactly 65475 bytes before anyone noticed.

Two checks, both cheap, and neither optional:

- Reject any response whose body begins `<!doctype html>`. A markdown source never does.
- Compare sizes across the batch. Identical byte counts for pages that should differ means you
  collected the same fallback several times over.

Whatever fails these is **a gap, not coverage**. Record it in `SOURCES.md` — which pages could not be
retrieved and what was tried — and say so in the report. A category checked against the wrong document
is worse than one openly skipped, because it will be counted as verified.

#### Follow the links out of the changelog

**The changelog is not the documentation.** On the first Binance run, the single most consequential
finding — a WebSocket migration whose deadline had already passed — was not a changelog entry at all.
It lived on its own page, reachable only through one link inside the changelog.

So: read the changelog, then extract its internal links and fetch those too. A vendor announcing
something large tends to write it up separately and link to it, which is exactly the shape a
changelog-only check misses.

Write `SOURCES.md` beside the files: for each, the URL, the tier, the fetch date, the size, and the
pinned revision where there is one. Then diff against the previous run's snapshot directory and read
what moved. On a first run there is nothing to diff against — read the changelogs whole, and say in the
report that this run established the baseline rather than measured a change.

### Phase 3 — diff, category by category

Walk the manifest in order. Every entry gets exactly one outcome:

| outcome | meaning |
|---|---|
| **unchanged** | still as recorded. Say so — knowing what held is half the value |
| **changed** | record the old assumption, the new documented fact, and every `file:line` carrying the old one |
| **deprecated** | still works, announced for removal at a date **still in the future** |
| **new** | the exchange added something we do not use and arguably should — a filter, an order type, a field carrying information we currently derive |
| **undocumented** | we depend on something the documentation does not state |

**Every date in the documentation is read against today's date.** An announced removal is only
`deprecated` while its date is ahead of us. Once that date has passed the entry is `changed`, and
almost certainly blocking: the thing was withdrawn and we did not move. The first Binance run found a
WebSocket migration deadline four months in the past, still described in the notice in the future
tense, because the notice was written before it. Documentation states dates; only the reader supplies
the present.

**The undocumented ones are the finding, not the footnote.** They were inferred from observed
behaviour, they can change with no changelog entry, and no future drift check will catch them in
advance. Flag every one, including those that still hold, and say what would happen if it stopped
holding. A check that only reports changes will never mention them, and they are the facts most likely
to break silently.

Read categories in this order, most consequential first: endpoints and auth, then request parameters,
then enumerations and error codes, then response fields, then filters, then rate limits, then timing.

### Phase 4 — report

Write `kb/providers/<provider>/<YYYY.MM>/<YYYY.MM.DD>-contract.md`, beside the snapshot it was written from. Immutable once written.

**The report is written against the stored snapshot, never against the live site.** That is what lets a later reader check the reasoning instead of taking it on trust, against a page that has since moved.

Group by outcome, severity first — a changed endpoint or error code outranks a new optional field.

For each **changed** entry, state **what will actually happen in our code**: an order rejected, a field
silently null, a filter unrecognised so the instrument is dropped entirely, a status folded into
`UnknownError`. That consequence is what makes the finding actionable and what tells the reader whether
it blocks the exchange run. A diff alone does not.

End with a plain statement: does anything found block running against the exchange?

### Phase 5 — write back

Update `manifest.md` **in place**: correct the changed facts, adjust markers, set `checked_against` to
today, and append one line to `status.md`'s reconcile history pointing at the report.

Where drift implies work in layers 2–5, write it into `status.md`'s rows for those layers as their
remediation spec. Do not fix it here. This layer owns the contract; the fixes belong to the layers that
carry it, and mixing them costs the ability to ask what the exchange changed, separately from what we
did about it.

**Commit the manifest edit, the status edit, the report and its snapshot together, in one commit.** A
manifest changed without the snapshot it was changed from leaves a fact with no evidence behind it.
Code fixes go in separate commits, so the manifest's history stays a history of the exchange rather
than of our repairs.

### Phase 6 — gate

Present: what was checked, what held, what drifted with its consequence, and whether anything blocks
the exchange run. Stop. Remediation of the other layers is the parent's business and the user's call.

## Manifest categories

The sweep and the diff both walk these, in this order:

1. **Endpoints** — every base URL, path and HTTP method, per market type.
2. **Auth and signing** — the algorithm, exactly what is signed, header names, timestamp source,
   validity window, and any stream-key lifecycle including which HTTP method extends it.
3. **Request parameters** — every parameter sent per endpoint, with hard-coded values called out. A
   hard-coded value is an assumption about the exchange wearing the costume of a constant.
4. **Response fields** — every JSON property read, per response type, including positional arrays,
   where the index *is* the contract and nothing protects it.
5. **Filters and limits** — instrument filters by their exact type strings, which field each populates,
   and what happens when one is absent. Absence behaviour is frequently the thing that changed.
6. **Enumerations** — every string value mapped to a domain enum, in both directions, with literals.
7. **Error and status codes** — every numeric code mapped and every HTTP status treated specially.
8. **Rate limiting** — header names, configured ceilings, decay, and whether the arithmetic is
   self-consistent.
9. **Timing and lifecycle** — intervals, page sizes, query windows, keepalive cadences, session limits.
10. **Hard-coded exchange facts** — magic numbers and defaults that mirror an exchange limit, wherever
    they sit.

For each entry also record, where it applies: `[DIVERGES]` between market types, `[DUPLICATED]` across
files, `[DEAD]` if unreachable from production. All three change what a later change costs, and all
three are invisible from any single file.

## What this layer does not do

- It does not call the exchange. Provenance is upgraded to `[LIVE]` by layer 6, from an actual response.
- It does not resolve a disagreement between two sources by picking one. Two sources that contradict
  each other are **unresolved**, recorded as such, until a third settles it — a search summary claiming
  a change the changelog does not contain is not a finding.
- It does not fix drift. It specifies it.
- It does not judge our code's correctness — only whether it matches an external fact. A correct
  implementation of a changed API and an incorrect implementation of an unchanged one are different
  problems, and conflating them is how a drift check turns into an argument.

## When a drift check is not the answer

If a test fails and the manifest says the relevant fact is unchanged and documented, the failure is ours.
Do not keep re-reading the exchange's documentation hoping to find an excuse. This check narrows the
search; it is not somewhere to hide.
