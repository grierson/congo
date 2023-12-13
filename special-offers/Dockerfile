FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "SpecialOffers.API/SpecialOffers.API.fsproj"
WORKDIR "/src"
RUN dotnet build "SpecialOffers.API/SpecialOffers.API.fsproj" -c Release -o /api/build

FROM build AS publish
WORKDIR "/src"
RUN dotnet publish "SpecialOffers.API/SpecialOffers.API.fsproj" -c Release -o /api/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /api/publish ./api
ENTRYPOINT dotnet api/SpecialOffers.API.App.dll
