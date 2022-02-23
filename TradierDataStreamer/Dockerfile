#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["TradierDataStreamer/TradierDataStreamer.csproj", "TradierDataStreamer/"]
COPY ["StockDataLibrary/StockDataLibrary.csproj", "StockDataLibrary/"]
RUN dotnet restore "TradierDataStreamer/TradierDataStreamer.csproj"
COPY . .
WORKDIR "/src/TradierDataStreamer"
RUN dotnet build "TradierDataStreamer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TradierDataStreamer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TradierDataStreamer.dll"]