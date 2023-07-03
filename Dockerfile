FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview AS build
WORKDIR /App
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/nightly/aspnet:8.0-preview
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "Grimoire.dll"]