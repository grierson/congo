.PHONY: test

test:
	dotnet test SpecialOffers.Tests/ --nologo --verbosity m

local:
	dotnet run --project SpecialOffers/SpecialOffers.fsproj

run:
	docker run --rm -p 5001:80 special-offers:latest
