---
name: implement-provider-contract
description: Reconcile an exchange provider's wire contract — derive every API fact the module depends on, verify the ledger still matches the code, diff it against the exchange's current documentation, and report what drifted and what it costs us. This is the drift check. Use as layer 1 of implement-provider, or standalone when the user says "сверь контракт", "проверь дрейф API", "check the provider contract", or when an exchange test fails for a reason that might not be our defect.
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

## The ledger

`kb/providers/<provider>.md` — living, mutable. One per exchange, with a shared section and a section
per market type, because the divergences between market types are the drift-prone part and are only
visible when both halves sit on the same page.

This layer owns the **contract manifest** section and the **reconcile history** index. It writes the
other layers' drift into their status sections but does not own them.

## Workflow

### Phase 0 — establish the tree

Clean and on `main` level with `origin/main`, or on `feature/<provider>`. Anything else stops the run;
report the actual state and ask. Never stash, never force, never hard-reset.

### Phase 1 — derive, or verify, the manifest against the code

**On a first run** there is no ledger. Derive the manifest from the code: sweep the provider's tree and
record every fact that belongs to the exchange, anchored to `file:line`, in the categories below. Mark
everything `[UNVERIFIED]` — derived from our code, not yet checked against anything. Set
`checked_against: never`. This is an inventory, not yet a baseline.

**On every later run**, verify the ledger still describes *this* code before comparing it to anything
external:

- **Every anchor resolves to what it claims.** Line numbers move with every edit, and a ledger
  pointing at the wrong line is worse than none — it will be trusted. Fix the ones that moved.
- **Every assumption in the code has an entry.** Sweep for new ones. This gap is the blind spot the
  document exists to close, and it opens quietly: a converter gains a field, nobody updates the ledger,
  and that field is now outside every future drift check.
- **Every entry still corresponds to live code.** An entry for something deleted becomes `[DEAD]` or
  goes.

Delegate this to a fresh subagent given the ledger and the tree. A reviewer who wrote the ledger will
read what it meant to say.

### Phase 2 — fetch the exchange's current documentation

Search for the current locations rather than trusting a URL written down earlier: documentation sites
move, which is the same class of drift this check is for.

Where an exchange publishes separately per market type, **fetch each**. The divergences are where drift
hides: a rename on one venue and not the other produces a failure that looks venue-specific and
therefore looks like our bug.

Read the **changelog first** where one exists. It says what changed and when, which is faster and more
reliable than diffing whole reference pages.

Cover, at minimum, every category the manifest has entries for.

### Phase 3 — diff, category by category

Walk the manifest in order. Every entry gets exactly one outcome:

| outcome | meaning |
|---|---|
| **unchanged** | still as recorded. Say so — knowing what held is half the value |
| **changed** | record the old assumption, the new documented fact, and every `file:line` carrying the old one |
| **deprecated** | still works, announced for removal. Record the date if given |
| **new** | the exchange added something we do not use and arguably should — a filter, an order type, a field carrying information we currently derive |
| **undocumented** | we depend on something the documentation does not state |

**The undocumented ones are the finding, not the footnote.** They were inferred from observed
behaviour, they can change with no changelog entry, and no future drift check will catch them in
advance. Flag every one, including those that still hold, and say what would happen if it stopped
holding. A check that only reports changes will never mention them, and they are the facts most likely
to break silently.

Read categories in this order, most consequential first: endpoints and auth, then request parameters,
then enumerations and error codes, then response fields, then filters, then rate limits, then timing.

### Phase 4 — report

Write `kb/providers/reports/<YYYY.MM>/<YYYY.MM.DD>-<provider>-contract.md`. Immutable once written.

Group by outcome, severity first — a changed endpoint or error code outranks a new optional field.

For each **changed** entry, state **what will actually happen in our code**: an order rejected, a field
silently null, a filter unrecognised so the instrument is dropped entirely, a status folded into
`UnknownError`. That consequence is what makes the finding actionable and what tells the reader whether
it blocks the exchange run. A diff alone does not.

End with a plain statement: does anything found block running against the exchange?

### Phase 5 — write back

Update the ledger **in place**: correct the changed facts, adjust markers, set `checked_against` to
today, append one line to the reconcile history pointing at the report.

Where drift implies work in layers 2–5, write it into those layers' status sections as their
remediation spec. Do not fix it here. This layer owns the contract; the fixes belong to the layers that
carry it, and mixing them costs the ability to ask what the exchange changed, separately from what we
did about it.

**Commit the ledger and its report together, in one commit.** Code fixes go in separate commits.

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
- It does not fix drift. It specifies it.
- It does not judge our code's correctness — only whether it matches an external fact. A correct
  implementation of a changed API and an incorrect implementation of an unchanged one are different
  problems, and conflating them is how a drift check turns into an argument.

## When a drift check is not the answer

If a test fails and the ledger says the relevant fact is unchanged and documented, the failure is ours.
Do not keep re-reading the exchange's documentation hoping to find an excuse. This check narrows the
search; it is not somewhere to hide.
