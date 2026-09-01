---
name: implement-provider
description: Drive an exchange provider module to a target state, step by step — derive what the code assumes, collect the provider's actual API facts and the drift between them, then wire types, provider and connector, each with its tests and its own live validation — assessing every step against the contract, remediating only the drift, and stopping at a human gate between steps. Use when onboarding a new exchange, when reconciling an existing one after the exchange changed its API, when the user says "реализуй провайдера <exchange>", "сверь провайдера с документацией", "implement the <exchange> provider", "check the provider for drift", or when an exchange test fails for a reason that might not be our defect.
user-invocable: true
---

# Implement Provider

Drive an exchange provider module to a **target state**, step by step. This skill is a
**reconciler**, not a one-shot builder: every call *assesses* each step's current implementation
against its target, *remediates* only the drift, *verifies*, and stops at a **human gate** before
advancing. A first run on a new exchange, a resumed run, and a re-run after the exchange changed its
documentation all converge through the same loop.

## Safety — this repository trades

Some of what this skill validates places **real orders on a real account**.

- **NEVER set `FINANCE_EXCHANGE_TESTS` on your own.** Steps 4 and 5 carry the live runs and each stage
  is a human gate. The variable is set by the user, or by you only when the user has approved *that stage*
  in *that call*.
- `test.env` files hold real credentials. Never read them for their values, never print them, never
  commit them.
- Steps 1-3 need no provider access at all. Steps 4 and 5 do, and stage themselves accordingly:
  step 4's live validation only reads, step 5's trades.

## The load-bearing idea: one contract, ported into every step

Step 1 produces the **wire contract** — every fact about the exchange's API that this module depends
on: endpoints, auth and signing, request parameters, response field names, filters, enumerations,
error codes, rate limits, stream payloads. It lives in the provider's `manifest.md` and it is the **single
target** every later step is measured against.

Steps 3–5 are independent transcriptions of that same contract into C#. They are consistent with
**the exchange**, not with each other — each is checked against the contract, never against a sibling.
That is what makes a divergence detectable: if converters and query processors were checked against
one another, a shared misreading would look like agreement.

When the exchange changes something, step 1 re-converges **first**, and the drift propagates outward
from there. Step 1 is therefore not preamble; it is the thing the rest copy.

## Two axes: what the provider says, and what we prove

Every fact carries **two independent states**, and squashing them into one marker is the mistake this
model exists to prevent. They answer different questions, they are changed by different steps, and a
fact can be strong on one axis and worthless on the other.

**Documentation** — what the provider says about it:

| state | meaning |
|---|---|
| `confirmed` | documented, and checked against that documentation at the manifest's `checked_against` date |
| `unchecked` | documented as far as we know, but not compared since it was written down |
| `undocumented` | **we depend on it and the documentation does not state it.** Inferred from observed behaviour, so it changes with no changelog entry and no drift check will catch it in advance |
| `contested` | two sources disagree. Not a finding and not settled — see the rule below |
| `unretrievable` | the page exists but no technique available reaches it at a fidelity worth storing |

**Verification** — what of ours would notice if it stopped being true:

| state | meaning |
|---|---|
| `pinned` | an offline test fails if this changes |
| `live` | observed in an actual provider response, with the date |
| `gated` | a test covers it, but that test only runs against the provider and is skipped by default — so it proves nothing on an ordinary run |
| `none` | nothing exercises it |
| `vacuous` | a test names it and **cannot fail** — an assertion over a collection that may be empty, a wait that swallows its own timeout, a negative check with no positive control. Worse than `none`, because it is counted as coverage |

Neither is a boolean, and both take a note. "Documented, but only in a changelog entry and never in the
reference" and "documented in full" are both `confirmed` and are not the same thing; the note is where
that lives.

### Why the pair, and not a single verdict

The combinations are what make it useful:

- **documented + pinned** — solid. Cheap to keep so.
- **documented + none** — we believe the provider and nothing catches our misreading of it. The most
  common shape, and the cheapest to fix, because a test can be written from the documentation.
- **undocumented + pinned** — our test pins behaviour the provider never promised. It will keep passing
  right up until the behaviour changes, and then it will fail with no explanation available anywhere.
- **undocumented + none** — pure exposure. These are the entries to count, and a manifest that cannot
  produce that count is not doing its job.
- **anything + vacuous** — worse than the same thing with `none`, because the effort looks spent.

### Which step moves which axis

The documentation axis is step 2's, and only step 2's. The verification axis starts in step 1 — which
records what tests exist today as it derives the facts — and is moved afterwards by steps 3, 4 and 5 as
they write tests and run live validation. A fact becomes `live` only from an actual response, which is
why steps 4 and 5 write back into the manifest rather than merely reading it.

### Marking

Only exceptions are written out. Where a whole category shares a state, say it once at the category's
head and mark what differs — the reader needs the list of what cannot be trusted, not a restatement of
everything. A category that cannot state a shared position for either axis is a category nobody has
finished.

Structural properties are orthogonal to both axes and stay as their own markers: `[DIVERGES]` between
market types, `[DUPLICATED]` across files, `[DEAD]` for code unreachable in production.

### Contested

Two sources disagreeing is a state, not a finding, and not something to resolve by preferring the more
recent or the more official. Record both readings and what would settle it. A drift check that picks a
side manufactures drift instead of finding it.

## Convergence model — assess → remediate → verify → gate

Idempotence here means **convergence, not abort**. There is no "already exists → skip". Each step
runs the same loop on every call:

1. **Assess.** Measure the step's current implementation against its target — the contract for steps
   3–5, the provider's documentation for steps 1–2. Delegate the judgement to a **fresh verifier
   subagent** given the contract, the step's files, and the step's done-checklist. A fresh
   adversarial context is what makes re-runs converge instead of manufacturing new work; a reviewer
   carrying findings from the last pass will always find more.
2. **Remediate**, only if there is drift. The drift list **is** the remediation spec — a scoped
   work-list, not a rebuild. Record it in `status.md` so an interrupted run resumes exactly there.
3. **Verify.** Re-assess. The step is converged only when its checklist passes.
4. **Gate.** Stop. Builds, credentials, exchange access and money are the user's. **Never advance
   while the current step has open drift** — a later step's target is the contract, and an
   unconverged contract is not a target worth porting.

A converged step re-assessed later is a no-op. A step whose target moved surfaces fresh drift.

## State — the per-provider documents

```
kb/providers/<provider>/
  manifest.md                     living — the contract. Changes when the exchange changes
  status.md                       living — convergence per step, drift, run history. Changes when we work
  <YYYY.MM>/
    <YYYY.MM.DD>-<step>.md        the run's report. Immutable
    <YYYY.MM.DD>-docs/            the documentation snapshot the report was written from
      SOURCES.md                  per file: URL, retrieval tier, date, size
      <venue>/<page>.md
```

One directory per exchange, with the manifest covering the shared code and each market type in its own
section. They belong in one manifest because the divergences between market types are the drift-prone
part, and a divergence is only visible when both halves sit on the same page.

**Manifest and status are separate on purpose.** They change for different reasons and at different
rates: the manifest when the exchange moves, the status when we do work. In one file, every run would
edit the document for two unrelated reasons and `git log -p manifest.md` would stop answering "what did
the exchange change" — which is the question the whole arrangement exists to answer.

### The documentation snapshot

Each run stores the documentation it read, beside its report, converted to markdown. This is what turns
a drift check from *reading against a moving target* into a **diff between two snapshots** — the next
run compares directories and reads only what moved.

Retrieval works in tiers, and the tier is recorded per file in `SOURCES.md` because it decides how much
a snapshot is worth:

| tier | source | fidelity |
|---|---|---|
| 1 | the vendor's own git repository | exact, and a commit SHA can be pinned |
| 2 | a docs site that serves markdown | exact |
| 3 | a summarising fetch, where neither of the above exists | **lossy** — a model's reading, not the page |

Tiers are how a snapshot is *obtained*; they are not what the process rests on. Most providers will
have no documentation repository at all, and the process must not degrade when one is missing — the
snapshot directory in **our** repository is the normalising layer, and the history is ours either way.
A report written from a tier-3 snapshot says so.

Snapshot only what the manifest references, plus changelogs. A vendor's full documentation set is
mostly about things we do not use, and volume that nobody diffs is volume that hides the diff.

### Revisions

Three levels, each answering a different question:

- **Git** answers *what changed* — `git log -p kb/providers/<provider>/manifest.md`, kept clean by the
  split above.
- **The dated report** answers *why*, and records for each finding **what it means in our code** — an
  order rejected, a field silently null, a status folded into `UnknownError`. That consequence is what
  makes a finding actionable; a diff alone is not.
- **The history table** in `status.md` answers *when*.

Two rules keep it readable:

- **One commit per run, carrying the report, its snapshot, and the manifest and status edits.** A
  manifest changed without the snapshot it was changed from leaves a fact with no evidence behind it.
- **Code fixes go in separate commits.** Drift creates work, but that work must not land in the same
  commit, or the manifest's history becomes a history of our repairs rather than of someone else's
  contract.

## Preflight

Before reading anything, establish the state of the tree. Assessment measures what is checked out, and
remediation writes to it; both are meaningless on a tree that is not what it claims to be.

Require **one** of: on `main`, clean and level with `origin/main` (behind-only → `git pull --ff-only`);
or on `feature/<provider>`, clean. Anything else — uncommitted changes, a detached HEAD, a third
branch, `main` diverged — is a **stop**: report the actual state and ask.

Never stash, never `checkout -f`, never `reset --hard`, never pull on a diverged branch. An interrupted
earlier run and unrelated work in progress look identical from here, and one of them is unrecoverable.
A tree dirty with this provider's own in-flight work is the common case after an interruption; it is
still a stop, and the answer is usually "yes, continue" — one question, and the only way this skill
could destroy work is gone.

Assessment is read-only and runs on whatever is checked out. **Remediation never writes on `main`**:
branch to `feature/<provider>` before the first edit. A step with no drift is never branched, so a
converged pass leaves the tree exactly as it found it — which makes "no-op" observable rather than
asserted.

Committing, pushing and merging are the user's unless asked for in that call.

## Steps

Five, and the first two are the contract. Registration, configuration and live validation are **not**
steps of their own: each belongs to the step whose work it serves, and separating them was how a
connector ended up configured in one place and implemented in another.

### Step 1 — derive the existing state from the code ✅ `implement-provider-contract`

**Target.** What this codebase currently believes about the provider's API, written down: every
endpoint, parameter, field name, enumeration, code and limit it depends on, each anchored to
`file:line`. On a greenfield provider this is **empty**, and saying so explicitly is the result — an
empty derivation and an unasked question look identical later.

Everything derived here is `unchecked` on the documentation axis by construction: it is what *we*
think, not what is true.

**Step 1 also sets the verification axis**, because it is the step that reads the code and can see what
tests exist. For each fact: is it `pinned` by an offline test, `gated` behind a provider-only suite,
`vacuous` — named by a test that cannot fail — or `none`. That census is cheap here and expensive
later, and without it the manifest cannot answer the question worth asking: how many facts do we depend
on that neither the provider documents nor any test of ours defends.

**Done.** Every anchor resolves to the line it claims — line numbers move with every edit, and a
manifest pointing at the wrong line is worse than none because it will be trusted. Every assumption in
the code has an entry; a converter that gained a field nobody recorded is outside every future check.
Every entry still corresponds to live code. Every entry carries a verification state, at category
granularity with the exceptions written out.

**→ GATE.** Present the derived state. The user confirms it describes the implementation before
anything is compared against the outside world.

### Step 2 — collect the provider's actual API facts, and compute the drift ✅ `implement-provider-contract`

**Target.** The current documented truth, snapshotted, and every entry from step 1 given an outcome
against it: unchanged, changed, deprecated, new, or undocumented.

**Completeness is the gate condition, not a quality goal.** Every category must be covered and every
entry must have an outcome. A partial collection cannot be built on: a step-3 transcription measured
against a half-checked contract inherits the unchecked half as silent assumption, and no later step
will ever question it. If a source cannot be retrieved, that is a **gap**, it is recorded as one, and
**it blocks**. Do not proceed on "the important parts were checked" — the parts nobody checked are the
ones nobody will think to check again.

**Done.** Every entry has an outcome and a documentation state — including the two that are easy to
skip: `undocumented`, for what we depend on and the provider never promised, and `contested`, where two
sources disagree. Every source fetched is stored beside the report with its retrieval tier; every
source that could not be is named, with what was tried. Nothing is counted as `confirmed` that was
checked against a document other than its own.

Report the counts, because a list nobody totals is a list nobody acts on: how many facts are
`undocumented`, how many are `undocumented + none`, and how many carry a `vacuous` test.

**→ GATE.** Present the drift and the gaps. The user confirms the picture is complete enough to build
on — which, given the paragraph above, normally means there are no gaps left.

### Step 3 — wire types and serialization ⬜ no child skill yet

**Target.** The types that carry the wire format, and the code that reads and writes it, **with their
tests**, built from what steps 1 and 2 established.

The format is not necessarily JSON — a provider may speak protobuf, FIX, SBE, msgpack, or a
positional text encoding. The step is *serialization*, and the transcription is measured against the
contract, never against a sibling implementation.

**Done.** Each fact this step covers moves from `none` to `pinned`, in the manifest as well as in the
code — a test written and not recorded leaves the manifest understating what we defend, which is the
same defect as overstating it, pointed the other way.

Every field in the contract is read; every field read is in the contract — the second
direction is what catches the fields we invented. Enumerations map every documented value in both
directions. Positional payloads have their indices pinned, because there the index *is* the contract
and nothing else protects it. Tests green.

### Step 4 — provider: the read paths ⬜ no child skill yet

**Target.** Exchange information, candles, account, orders, trades — with their tests. **If the test
project does not exist, it is created as part of this step**, not deferred.

Registration, endpoints and configuration for everything this step touches are done **here**, as it is
built. A read path whose endpoint is configured in a later step is a read path that cannot be tested
in this one.

**Fix what the census says nothing watches, and pin it in the same change.** A fact at `none` that turns
out to be wrong was found by review, and review is not repeatable. Repairing it without a test returns
it to the state it was found in — the next drift is invisible again, and the repair is the only reason
anyone believes otherwise.

**Assert the composed value, not the configured parts.** Configuration that reads as correct can compose
into something else: on this provider the websocket route had to live in the path rather than the base,
because `new Uri(base, path)` discards the base's path whenever the path begins with a slash — so a
route held in the base is dropped at composition and the decommissioned URL comes back from settings
that look right. A test on the parts passes; a test on the composed URL catches it. Where a value is
assembled before use, the assembled value is the fact.

**Live validation belongs to this step too, and it reads only.** Signing, server time, public market
data, then authenticated account reads. Nothing here places an order, which is what makes it the safe
half of validation — and the reason it comes before step 5 rather than after it.

**Done.** Facts this step exercises offline become `pinned`; facts a read-only live stage observed
become `live`, dated. Every endpoint called with exactly the contract's parameters. Paging and windows match the
documented caps. Failure paths return something the caller can act on rather than an empty success.
Tests green offline; the read-only live stages pass.

### Step 5 — connector: streams and the order lifecycle ⬜ no child skill yet

**Target.** Subscriptions, the sync cycle, status reporting, place / modify / cancel — with their
tests, and with their registration and configuration done here as they are built.

**Live validation belongs to this step too, and this is the half that trades.** It is gated
separately, per stage, and never advances on the previous stage having passed. Before anything places
an order, confirm with the user: the account's position mode, that no position exists on the test
symbol which the fixture would close as "cleanup", and sufficient margin. Run the trading suite
**alone** — nothing else against the same account concurrently.

**Done.** Facts this step exercises become `pinned`, and those an approved live stage observed become
`live`, dated — including the ones only a placed order can settle. Every stream event handled. Status transitions map to the domain's vocabulary. Errors reach
the connector's error channel rather than a log line — a connector that fails silently is
indistinguishable from one that is merely reconnecting. Tests green; the live stages pass, each
approved in turn.

## Provenance, and which step supplies it

Step 1 derives each fact and records what tests pin it today. Step 2 sets its documentation state.
Steps 3, 4 and 5 move its verification state as they write tests and observe live responses. A
contract whose facts are all `documented + none` is not wrong — it is unproven, and the manifest says
so per fact rather than in aggregate.

## Orchestration

Run the preflight, read `manifest.md` and `status.md`, then reconcile each step in order. For each: invoke its child
skill in reconcile mode, or — where no child exists — run the verifier subagent against that step's
checklist, write the drift into `status.md` as a printed hand-off spec, and **stop**. A missing child
skill degrades to an honest hand-off; it never silently skips.

Thread `<provider>` and the provider's kb directory through every step. Only advance on the user's go,
and only when the step is verified converged.

## Arguments

```
/implement-provider <provider> [--from-step=N] [--only-step=N] [--docs=<url or path>]
```

- `<provider>` — the module name as it appears under `providers/`, e.g. `binance`. Ask if missing.
- `--from-step=N` — start at step N. Still refuses to advance past an earlier step `status.md` marks
  unconverged; use `--only-step` to override deliberately.
- `--only-step=N` — reconcile just that step.
- `--docs` — where the exchange's current documentation lives, forwarded to step 1.

## Error recovery

1. A child skill reports its own failures; the parent surfaces them and stops at that step's gate,
   with the drift in `status.md` so the next call resumes there.
2. Never advance past a gate on the user's behalf.
3. Reconcile is order-strict: refuse to remediate step N+1 while step N has open drift.
4. Never auto-retry anything that writes, and never retry an exchange call that may have placed an
   order — read the account instead.
5. A failed preflight stops the pass. Report the actual state and ask.

## Writing the remaining child skills

Steps 3-5 have no child skill yet, and that is deliberate. **Drive a step by hand once, then write
its skill.** A checklist written from reading the code is always missing the items that only appear on
contact; the parent's degraded hand-off is good enough until then, and an incomplete child skill is
worse than none because it looks authoritative.

## Where a provider actually stands

The manifest and status files' answer, never this one. Per-provider progress written into a skill goes stale the moment
the next run advances it, and a stale claim here contradicts the documents a reconcile pass trusts.
