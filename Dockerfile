FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine
RUN apk add --no-cache curl unzip

RUN curl -OJL https://github.com/Yucked/Grimoire/archive/refs/heads/main.zip && \
    unzip '*.zip' && \
    mkdir app && mv Grimoire-main/* app/ && \
    rm *.zip  && rm -rf Grimoire-main

WORKDIR app
RUN dotnet restore && \
    dotnet publish -c Release -o out

WORKDIR out
ENTRYPOINT ["dotnet", "Grimoire.Web.dll"]
