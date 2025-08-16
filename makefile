setup:
	$(call header)
	dotnet tool restore

format:
	$(call header)
	dotnet tool run csharpier format . --config-path $(shell pwd)/.editorconfig
	dotnet tool run xs format -sc -ic

format-full: format
	$(call header)
	dotnet format style
	dotnet format analyzers

ensure-no-changes:
	$(call header)
	@if [[ -n "$$(git status --porcelain)" ]]; then \
		echo "Changes detected:"; \
		git status; \
		git --no-pager diff --no-color --exit-code; \
	fi

update:
	$(call header)
	dotnet tool list --format json | jq -r '.data[] | "\(.packageId)"' | xargs -I% dotnet tool install %
	dotnet tool run xs update all -sc -ic

clean:
	$(call header)
	dotnet tool run xs clean -sc -ic
	find . -type f -name '*.nupkg' | xargs -I% rm %

build:
	$(call header)
	$(call get-package-version)
	dotnet build -c Release --nologo -v q -p:PackageVersion=$(packageVersion)

test:
	$(call header)
	dotnet test -c Release --no-build --nologo --logger "trx;LogFilePrefix=test-results.trx"

pack:
	$(call header)
	$(call get-package-version)
	dotnet pack --no-build -o . -c Release -p:SymbolPackageFormat=snupkg -p:PackageVersion=$(packageVersion)

publish:
	$(call header)
	dotnet nuget push "*.nupkg" --source https://dotnet.pkg.annium.com/v3/index.json --api-key $(shell cat .xs.credentials)
	find . -type f -name '*.nupkg' | xargs -I% rm %

docs-lint:
	$(call header)
	dotnet tool run doclint lint -w . -i '**/*.cs' -e '**/obj/**/*.cs'

docs-clean:
	$(call header)
	rm -rf _site api

docs-metadata:
	$(call header)
	dotnet tool run docfx metadata docfx.json

docs-build:
	$(call header)
	dotnet tool run docfx docfx.json

docs-serve:
	$(call header)
	dotnet tool run docfx serve _site

docs-watch:
	$(call header)
	dotnet tool run docfx docfx.json --serve

# CI
ci-merge-request-short:
	$(call header)
	make setup
	make format
	make ensure-no-changes
	make clean
	make build

ci-merge-request-full:
	$(call header)
	make setup
	make format
	make ensure-no-changes
# 	make docs-lint
	make clean
	make build
	make test
# 	make docs-build

ci-release:
	$(call header)
	make setup
	make format
	make ensure-no-changes
	make ci-set-package-version
	make clean
	make build
	make pack
# 	make docs-build
	make publish apiKey=$(apiKey)
	make ci-push-tag repository=$(repository) githubToken=$(githubToken)
	echo "Release complete"

ci-set-package-version:
	$(call header)
# 	git config user.name "it"
# 	git config user.email "it@annium.com"
	dotnet tool run versioning set-version -v $(shell cat version)

ci-push-tag:
	$(call header)
	$(call get-package-version)
# 	git remote set-url origin https://x-access-token:$(githubToken)@github.com/$(repository).git
	git push origin v$(packageVersion)


define header
	@echo "=== $@ ==="
endef

define get-package-version
	$(eval packageVersion := $(shell dotnet tool run versioning get-version -v $(shell cat version)))
endef

.PHONY: $(MAKECMDGOALS)
