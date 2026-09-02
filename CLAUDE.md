# CLAUDE.md

Guidance for Claude Code (claude.ai/code) working in this repository.

## What this repository is

`Annium.Finance` — exchange provider integrations. A **provider** is one venue's market type: Binance
spot and Binance USD-M futures are two providers, not one. Each speaks to a real exchange over REST and
websockets, and the user half of one places real orders on a real account.

Twelve projects, `net10.0`, solution `Annium.Finance.sln`.

```
providers/
  base/
    src/    Abstractions.Domain · Abstractions.Connectors · Core
    tests/  the two offline suites, plus Tests.Lib — the shared fixture library
  crypto/binance/
    src/    Binance.Base (shared transport, signing, sockets) · Binance.Spot · Binance.UsdFutures
    tests/  one suite per assembly
kb/providers/<provider>/   the contract manifest and reconcile status per provider
.claude/skills/            implement-provider and its contract step
```

`base/src/Core` holds what every provider shares: the connector and provider base classes, the loaders,
the rate limiter, the status monitor. `Binance.Base` holds what both Binance venues share. Spot and
futures **diverge more than they look** — order-type wire strings, filter names, one letter of the trade
maker flag — so a change to one is not a change to the other. `kb/providers/binance/manifest.md` records
where.

## Commands

`just` is the entry point; `just` alone lists every recipe.

| command | what it does |
|---|---|
| `just setup` | restore dotnet tools (CSharpier, doclint, xs) |
| `just format` | CSharpier + `xs format` |
| `just build` | build in Release |
| `just test` | the offline block — see below |
| `just docs-lint` | XML documentation lint over every `.cs` |
| `just update` / `just clean` / `just pack` | packages, artifacts, NuGet packages |

`just test` runs with `--no-build`, so **build first** or you will test a stale binary. Running a single
class:

```
dotnet test --project <path>.csproj -c Release --no-build -- --filter-class '*SomeTests'
```

## Tests are in three blocks, and one of them trades

Sorted by an xunit trait on the class or a base of it — traits inherit, so marking a fixture base
carries every suite built on it.

| recipe | block | touches |
|---|---|---|
| `just test` → `test-offline` | unmarked | nothing outside the process |
| `just test-read` | `block=read` | real exchanges and real accounts, mutating nothing |
| `just test-write` | `block=write` | **places and cancels real orders, opens and closes positions** |

**The trait is the only thing separating a routine run from one that trades.** There is no second gate:
an environment variable each exchange test was checked against was dropped deliberately, since it
protected only against a trait being wrong and cost a variable on every legitimate run. A trait being
wrong is an accepted risk; marking a test is therefore part of writing it, not a later tidy-up.

Absence of a trait means offline — the safe default in the direction that matters, since a test nobody
marked joins the block that always runs rather than the one that never does.

`Exchange.HasCredentials` is not a permission but a condition of possibility: without `test.env` the
tests needing keys say so instead of failing on a missing one. `test.env` holds real credentials, is
gitignored, and only `.example` is tracked.

Before `just test-write`: check the account's position mode, that no position exists on the test symbol
the fixture would close as cleanup, and the available margin. Run it alone.

## Conventions

- Nullable enabled, **warnings as errors** — including IDE analyzers CI enforces and a local
  `dotnet test` will not surface. `just build` is the check that matters.
- XML documentation required on every member; `just docs-lint` enforces it. No `inheritdoc`.
- CSharpier formatting; run `just format` before committing.
- Central package management via `Directory.Packages.props`.
- Test naming `MethodName_Scenario_ExpectedResult`; assertions are Annium.Testing's fluent ones
  (`.Is()`, `.IsTrue()`, `.Has()`, `.IsEmpty()`).
- **`Expect.ToAsync` is the assertion; `Wait.UntilAsync` is not.** The latter swallows the cancellation
  its own timeout raises and returns normally, so a wait on it followed by a lenient check passes when
  the condition never held.
- Operations return `IResult<T>` / `MarketResult` / `UserResult` rather than throwing for business
  failures.

## Working on a provider

`/implement-provider <provider>` drives one to a target state in five gated steps: derive what the code
assumes, collect the provider's actual API facts and the drift between them, then wire types, provider
and connector, each with its tests and its own live validation. `kb/providers/<provider>/manifest.md` is
the contract every later step is measured against; `status.md` says how far it got.

Before debugging an exchange failure, ask whether the thing we depend on changed. The manifest exists so
that question is answerable in minutes rather than by re-reading our own code.
