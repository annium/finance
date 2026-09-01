set shell := ["bash", "-cu"]
set positional-arguments
# lib.just is copied in by the umbrella repo's `just copy-ci`; recipes redefined below
# override the shared ones.
set allow-duplicate-recipes := true

import 'lib.just'

[private]
default:
    @just --list

# base

setup:
    @echo "=== $0 ==="
    dotnet tool restore

format:
    @echo "=== $0 ==="
    dotnet tool run csharpier format . --config-path $(pwd)/.editorconfig
    dotnet tool run xs format -sc -ic

format-full: format
    @echo "=== $0 ==="
    dotnet format style
    dotnet format analyzers

ensure-no-changes:
    #!/usr/bin/env bash
    set -e
    echo "=== ensure-no-changes ==="
    if [[ -n "$(git status --porcelain)" ]]; then
        echo "Changes detected:"
        git status
        git --no-pager diff --no-color --exit-code
    fi

update:
    @echo "=== $0 ==="
    dotnet tool list --format json | jq -r '.data[] | "\(.packageId)"' | xargs -I% dotnet tool install %
    dotnet tool run xs update all -sc -ic

clean:
    @echo "=== $0 ==="
    dotnet tool run xs clean -sc -ic
    find . -type f -name '*.nupkg' | xargs -I% rm %

build:
    #!/usr/bin/env bash
    set -e
    echo "=== build ==="
    packageVersion=$(dotnet tool run versioning get-version -v $(cat version))
    dotnet build -c Release --nologo -v q -p:PackageVersion=$packageVersion

# the default run: everything that needs neither the exchange nor credentials. Blocks are additive by
# cost, so the cheap tests stay first and a failure in them is seen before anything expensive starts
test: test-offline

# a test belongs to a block by an xunit trait on its class or a base of it - see TestBlock. Absence means
# offline, which is the safe default in the direction that matters: a test nobody marked joins the block
# that always runs rather than the one that never does. The trait decides selection only; what keeps a
# trading test from reaching the exchange is its own SkipUnless gate, and that holds however it is marked
test-offline:
    @echo "=== $0 ==="
    dotnet test -c Release --no-build --report-xunit-trx -- --filter-not-trait "block=read" --filter-not-trait "block=write"

# connects to real exchanges and real accounts, and mutates nothing
test-read:
    @echo "=== $0 ==="
    FINANCE_EXCHANGE_TESTS=1 dotnet test -c Release --no-build --report-xunit-trx -- --filter-trait "block=read"

# places and cancels real orders, opens and closes real positions. Run it alone, on an account whose state
# you have just looked at, and never alongside anything else touching the same one
test-write:
    @echo "=== $0 ==="
    FINANCE_EXCHANGE_TESTS=1 dotnet test -c Release --no-build --report-xunit-trx -- --filter-trait "block=write"

pack:
    #!/usr/bin/env bash
    set -e
    echo "=== pack ==="
    packageVersion=$(dotnet tool run versioning get-version -v $(cat version))
    dotnet pack --no-build -o . -c Release -p:SymbolPackageFormat=snupkg -p:PackageVersion=$packageVersion

# docs

docs-lint:
    @echo "=== $0 ==="
    dotnet tool run doclint lint -w . -i '**/*.cs' -e '**/obj/**/*.cs'

docs-clean:
    @echo "=== $0 ==="
    rm -rf _site api

docs-metadata:
    @echo "=== $0 ==="
    dotnet tool run docfx metadata docfx.json

docs-build:
    @echo "=== $0 ==="
    dotnet tool run docfx docfx.json

docs-serve:
    @echo "=== $0 ==="
    dotnet tool run docfx serve _site

docs-watch:
    @echo "=== $0 ==="
    dotnet tool run docfx docfx.json --serve
