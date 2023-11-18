format:
	xs format -sc -ic
	dotnet csharpier .

setup:
	xs remote restore -user $(user) -password $(pass)
	dotnet tool restore

update:
	xs update all -sc -ic

clean:
	xs clean -sc -ic

build:
	dotnet build -c Release --nologo -v q

test:
	dotnet test -c Release --no-build --nologo -v q

publish:
	dotnet pack --no-build -o . -c Release -p:PackageVersion=0.1.0 -p:SymbolPackageFormat=snupkg
	dotnet nuget push "*.nupkg" --source https://dotnet.pkg.annium.com/v3/index.json --api-key $(shell cat .xs.credentials)
	find . -type f -name '*.nupkg' | xargs rm

.PHONY: $(MAKECMDGOALS)
