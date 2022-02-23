FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["ChonkyBot/ChonkyBot.csproj", "ChonkyBot/"]
COPY ["StockDataLibrary/StockDataLibrary.csproj", "StockDataLibrary/"]

RUN dotnet restore "ChonkyBot/ChonkyBot.csproj"
COPY . .
WORKDIR "/src/ChonkyBot"
RUN dotnet build "ChonkyBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChonkyBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChonkyBot.dll"]