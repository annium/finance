setup:
	dotnet tool restore

format:
	dotnet tool run csharpier format . --config-path $(shell pwd)/.editorconfig
	dotnet tool run xs format -sc -ic

format-full: format
	dotnet format style
	dotnet format analyzers

update:
	dotnet tool list --format json | jq -r '.data[] | "\(.packageId)"' | xargs -I% dotnet tool install %
	dotnet tool run xs update all -sc -ic

clean:
	dotnet tool run xs clean -sc -ic
	find . -type f -name '*.nupkg' | xargs rm

buildNumber?=0
build:
	dotnet build -c Release --nologo -p:BuildNumber=$(buildNumber)

test:
	dotnet test -c Release --no-build --nologo --logger "trx;LogFilePrefix=test-results.trx"

pack:
	dotnet pack --no-build -o . -c Release -p:SymbolPackageFormat=snupkg

publish:
	dotnet nuget push "*.nupkg" --source https://dotnet.pkg.annium.com/v3/index.json --api-key $(shell cat .xs.credentials)
	find . -type f -name '*.nupkg' | xargs rm

.PHONY: $(MAKECMDGOALS)
