#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["TDAStreamer/TDAStreamer.csproj", "TDAStreamer/"]
RUN dotnet restore "TDAStreamer/TDAStreamer.csproj"
COPY . .
WORKDIR "/src/TDAStreamer"
RUN dotnet build "TDAStreamer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TDAStreamer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TDAStreamer.dll"]