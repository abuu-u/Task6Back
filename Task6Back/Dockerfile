FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Task6Back/Task6Back.csproj", "Task6Back/"]
RUN dotnet restore "Task6Back/Task6Back.csproj"
COPY . .
WORKDIR "/src/Task6Back"
RUN dotnet build "Task6Back.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Task6Back.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Task6Back.dll"]
