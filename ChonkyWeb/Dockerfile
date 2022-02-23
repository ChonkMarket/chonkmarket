FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["ChonkyWeb/ChonkyWeb.csproj", "ChonkyWeb/"]

RUN curl -sL https://deb.nodesource.com/setup_10.x |  bash -
RUN apt-get install -y nodejs

RUN dotnet restore "ChonkyWeb/ChonkyWeb.csproj"
COPY . .
WORKDIR "/src/ChonkyWeb"
RUN dotnet build "ChonkyWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChonkyWeb.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChonkyWeb.dll"]