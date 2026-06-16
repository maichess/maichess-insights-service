FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

ARG GITHUB_ACTOR

COPY nuget.config ./
COPY MaichessInsightsService.csproj ./
RUN --mount=type=secret,id=GITHUB_TOKEN \
    GITHUB_TOKEN=$(cat /run/secrets/GITHUB_TOKEN) \
    dotnet restore MaichessInsightsService.csproj

COPY . .
RUN dotnet publish MaichessInsightsService.csproj \
    -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MaichessInsightsService.dll"]
