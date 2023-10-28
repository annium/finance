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
	dotnet build --nologo -v q

test:
	dotnet test --nologo -v q

publish:
	xs publish all 0.1.0 -p 1

.PHONY: $(MAKECMDGOALS)
