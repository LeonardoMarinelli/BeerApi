FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BeerApi.slnx .
COPY src/BeerApi.Domain/BeerApi.Domain.csproj src/BeerApi.Domain/
COPY src/BeerApi.Application/BeerApi.Application.csproj src/BeerApi.Application/
COPY src/BeerApi.Infrastructure/BeerApi.Infrastructure.csproj src/BeerApi.Infrastructure/
COPY src/BeerApi.Api/BeerApi.Api.csproj src/BeerApi.Api/
RUN dotnet restore src/BeerApi.Api/BeerApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/BeerApi.Api/BeerApi.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "BeerApi.Api.dll"]
