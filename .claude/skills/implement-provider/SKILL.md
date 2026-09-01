---
name: implement-provider
description: Drive an exchange provider module to a target state, layer by layer — wire contract, contracts and converters, provider, connector, registration, and the staged live validation against the exchange — by assessing each layer against the contract, remediating only the drift, and stopping at a human gate between layers. Use when onboarding a new exchange, when reconciling an existing one after the exchange changed its API, when the user says "реализуй провайдера <exchange>", "сверь провайдера с документацией", "implement the <exchange> provider", "check the provider for drift", or when an exchange test fails for a reason that might not be our defect.
user-invocable: true
---

# Implement Provider

Drive an exchange provider module to a **target state**, layer by layer. This skill is a
**reconciler**, not a one-shot builder: every call *assesses* each layer's current implementation
against its target, *remediates* only the drift, *verifies*, and stops at a **human gate** before
advancing. A first run on a new exchange, a resumed run, and a re-run after the exchange changed its
documentation all converge through the same loop.

## Safety — this repository trades

Some of what this skill validates places **real orders on a real account**.

- **NEVER set `FINANCE_EXCHANGE_TESTS` on your own.** Layer 6 stages the live runs and each stage is a
  human gate. The variable is set by the user, or by you only when the user has approved *that stage*
  in *that call*.
- `test.env` files hold real credentials. Never read them for their values, never print them, never
  commit them.
- Assessment and remediation of layers 1–5 need no exchange access at all. Only layer 6 does.

## The load-bearing idea: one contract, ported into every layer

Layer 1 produces the **wire contract** — every fact about the exchange's API that this module depends
on: endpoints, auth and signing, request parameters, response field names, filters, enumerations,
error codes, rate limits, stream payloads. It lives in the provider's ledger and it is the **single
target** every later layer is measured against.

Layers 2–5 are independent transcriptions of that same contract into C#. They are consistent with
**the exchange**, not with each other — each is checked against the contract, never against a sibling.
That is what makes a divergence detectable: if converters and query processors were checked against
one another, a shared misreading would look like agreement.

When the exchange changes something, layer 1 re-converges **first**, and the drift propagates outward
from there. Layer 1 is therefore not preamble; it is the thing the rest copy.

## The second target: what a fact is worth

A contract fact is not just a value. It carries **provenance**, and the provenance decides how much a
layer may rely on it:

| marker | meaning |
|---|---|
| *(unmarked)* | confirmed against documentation at the ledger's `checked_against` date |
| `[UNVERIFIED]` | derived from our own code or from memory, never checked against the exchange's docs |
| `[UNDOCUMENTED]` | we depend on it, the documentation does not state it. Inferred from observed behaviour — **changes without a changelog entry**, so no drift check will ever catch it in advance |
| `[LIVE]` | confirmed by an actual exchange response in a layer-6 run, with the date |
| `[DIVERGES]` | market types (spot / futures / …) assume different things here |
| `[DUPLICATED]` | encoded in more than one place, so a change must be made more than once |
| `[DEAD]` | encoded but unreachable from production today |

Only exceptions carry a marker. A fact confirmed at the last check needs none — `checked_against`
covers it. The reader needs the list of what *cannot* be trusted, not a list of everything.

**Provenance is upgraded by layer 6, not by layer 1.** This differs from the usual arrangement and is
worth stating plainly: our live validation is expensive and staged, so layer 1 can only ever produce
`doc-derived` facts. A fact becomes `[LIVE]` when an exchange run actually exercised it and layer 6
writes that back into the ledger. A contract with no `[LIVE]` facts is not wrong — it is unproven, and
the ledger says which.

## Convergence model — assess → remediate → verify → gate

Idempotence here means **convergence, not abort**. There is no "already exists → skip". Each layer
runs the same loop on every call:

1. **Assess.** Measure the layer's current implementation against its target — the contract for layers
   2–6, the exchange's documentation for layer 1. Delegate the judgement to a **fresh verifier
   subagent** given the contract, the layer's files, and the layer's done-checklist. A fresh
   adversarial context is what makes re-runs converge instead of manufacturing new work; a reviewer
   carrying findings from the last pass will always find more.
2. **Remediate**, only if there is drift. The drift list **is** the remediation spec — a scoped
   work-list, not a rebuild. Record it in the ledger so an interrupted run resumes exactly there.
3. **Verify.** Re-assess. The layer is converged only when its checklist passes.
4. **Gate.** Stop. Builds, credentials, exchange access and money are the user's. **Never advance
   while the current layer has open drift** — a later layer's target is the contract, and an
   unconverged contract is not a target worth porting.

A converged layer re-assessed later is a no-op. A layer whose target moved surfaces fresh drift.

## State — the per-provider ledger

```
kb/providers/<provider>.md
```

**Living, mutable reconcile state — not a dated snapshot.** One ledger per exchange, with a shared
section for what the exchange's common code holds and a section per market type. They belong in one
document because the divergences between market types are the drift-prone part, and a divergence is
only visible when both halves are on the same page.

The ledger holds three things:

- **Contract manifest** — the target. Every fact, anchored to `file:line`, marked per the provenance
  table above.
- **Convergence status** — per layer: `not-started | drift | converged`, the evidence, and the
  outstanding drift.
- **Reconcile history** — one line per run: date, link to that run's report, outcome. This answers
  "when did we last check and what came of it" without reading git.

### Revisions

Three levels, each answering a different question, and none of them optional:

- **Git** answers *what changed* — `git log -p kb/providers/<provider>.md` is the line-by-line history.
- **The dated report** answers *why*: `kb/providers/reports/<YYYY.MM>/<YYYY.MM.DD>-<provider>-<layer>.md`,
  one per run, never edited afterwards. It records findings and, for each, **what it means in our
  code** — an order rejected, a field silently null, a status folded into `UnknownError`. The
  consequence is what makes a finding actionable; a diff alone is not.
- **The history index** in the ledger answers *when*.

Two rules keep that history readable:

- **One commit per run, carrying both the ledger edit and its report.** A ledger changed without its
  report leaves a fact with no reason attached.
- **Code fixes go in separate commits.** Drift creates work, but that work must not land in the same
  commit — otherwise the ledger's history becomes a history of our repairs rather than of someone
  else's contract, and "what did the exchange change this year" stops being answerable.

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
branch to `feature/<provider>` before the first edit. A layer with no drift is never branched, so a
converged pass leaves the tree exactly as it found it — which makes "no-op" observable rather than
asserted.

Committing, pushing and merging are the user's unless asked for in that call.

## Layers

Each layer lists its target and the checklist a verifier measures against.

### Layer 1 — wire contract ✅ child skill: `implement-provider-contract`

**Target.** The ledger's contract manifest, complete and current: every fact the module depends on,
anchored, with provenance. **Assessing this layer against the exchange's documentation is the drift
check** — there is no separate skill for it.

**Done.** Every anchor resolves to the line it claims. Every assumption present in the code has an
entry. Every entry has an outcome from the last documentation pass. `checked_against` is today.

**Gate.** The manifest is converged and its drift, if any, is written as the remediation spec for the
layers that carry it.

### Layer 2 — contracts and converters ⬜ no child skill yet

**Target.** The `Contracts/` tree: the DTOs and `JsonConverter`s that transcribe the contract.

**Done.** Every response field in the manifest is read by a converter, and every field a converter
reads is in the manifest — the second direction catches the fields we invented. Enumerations map every
documented value in both directions. Positional payloads (a kline is an array, not an object) have
their indices pinned by a test. Offline converter tests green.

### Layer 3 — provider ⬜ no child skill yet

**Target.** The read paths: exchange info, candles, account, orders, trades.

**Done.** Every endpoint in the manifest that the module uses is called with exactly the manifest's
parameters. Paging windows and page sizes match the manifest's documented caps. Failure paths return a
result the caller can act on rather than an empty success.

### Layer 4 — connector ⬜ no child skill yet

**Target.** The streams and the order lifecycle: subscriptions, the sync cycle, status reporting,
place / modify / cancel.

**Done.** Every stream event in the manifest is handled. Status transitions map to the domain's
vocabulary. Errors reach `OnError` rather than a log line — a connector that fails silently is
indistinguishable from one that is merely reconnecting.

### Layer 5 — registration and configuration ⬜ no child skill yet

**Target.** Endpoints, keyed registrations, rate-limit configuration, DI wiring.

**Done.** Every configured limit matches the manifest, including the decay arithmetic. Every keyed
registration resolves. Nothing that is really an exchange fact is hard-coded where the manifest cannot
see it.

### Layer 6 — live validation ⬜ no child skill yet · **staged, each stage a gate**

This is the only layer that touches the exchange, and it is staged by the cost of being wrong. **Each
stage is a separate human gate.** Never run a stage the user has not approved in this call, and never
run a later stage because an earlier one passed.

| stage | what runs | touches |
|---|---|---|
| 6a | signing, server time | nothing — public endpoints |
| 6b | market connector, market provider | public market data |
| 6c | user provider | reads the account |
| 6d | **the connector suite** | **places and cancels real orders** |

Before 6d, confirm with the user: the account's position mode, that no position exists on the test
symbol that the fixture would close as "cleanup", and sufficient margin. Run 6d **alone** — nothing
else against the same account concurrently.

After each stage, write what it confirmed back into the manifest as `[LIVE]` with the date. That
write-back is this layer's real product: it is the only thing that upgrades a fact's provenance.

**Gate.** The user's explicit go, per stage.

## Orchestration

Run the preflight, read the ledger, then reconcile each layer in order. For each: invoke its child
skill in reconcile mode, or — where no child exists — run the verifier subagent against that layer's
checklist, write the drift into the ledger as a printed hand-off spec, and **stop**. A missing child
skill degrades to an honest hand-off; it never silently skips.

Thread `<provider>` and the ledger path through every layer. Only advance on the user's go, and only
when the layer is verified converged.

## Arguments

```
/implement-provider <provider> [--from-layer=N] [--only-layer=N] [--docs=<url or path>]
```

- `<provider>` — the module name as it appears under `providers/`, e.g. `binance`. Ask if missing.
- `--from-layer=N` — start at layer N. Still refuses to advance past an earlier layer the ledger marks
  unconverged; use `--only-layer` to override deliberately.
- `--only-layer=N` — reconcile just that layer.
- `--docs` — where the exchange's current documentation lives, forwarded to layer 1.

## Error recovery

1. A child skill reports its own failures; the parent surfaces them and stops at that layer's gate,
   with the drift in the ledger so the next call resumes there.
2. Never advance past a gate on the user's behalf.
3. Reconcile is order-strict: refuse to remediate layer N+1 while layer N has open drift.
4. Never auto-retry anything that writes, and never retry an exchange call that may have placed an
   order — read the account instead.
5. A failed preflight stops the pass. Report the actual state and ask.

## Writing the remaining child skills

Layers 2–6 have no child skill yet, and that is deliberate. **Drive a layer by hand once, then write
its skill.** A checklist written from reading the code is always missing the items that only appear on
contact; the parent's degraded hand-off is good enough until then, and an incomplete child skill is
worse than none because it looks authoritative.

## Where a provider actually stands

The ledger's answer, never this file. Per-provider progress written into a skill goes stale the moment
the next run advances it, and a stale claim here contradicts the one document a reconcile pass trusts.
