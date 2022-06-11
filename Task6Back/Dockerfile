FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish \
    --runtime alpine-$(uname -m | sed s/aarch64/arm64/ | sed s/x86_64/x64/) \
    --self-contained true \
    /p:PublishTrimmed=true

FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./Task6Back"]