FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY backend/Grimoire.csproj .
RUN dotnet restore

COPY backend/ .
RUN dotnet publish -c Release -o /app/out

RUN dotnet tool install --global Microsoft.Playwright.CLI && \
    /root/.dotnet/tools/playwright install --with-deps chromium

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache \
    chromium \
    chromium-chromedriver \
    nss \
    freetype \
    harfbuzz \
    ca-certificates \
    ttf-freefont

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "Grimoire.dll"]
