FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BgituGrades.csproj", "./"]
RUN dotnet restore "./BgituGrades.csproj"
COPY . .
RUN dotnet build "./BgituGrades.csproj" -c $BUILD_CONFIGURATION -o /src/build

FROM build AS publish
RUN dotnet publish "./BgituGrades.csproj" -c Release -o /src/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /src/publish .
COPY ["appsettings.Production.json", "./"]
ENTRYPOINT ["dotnet", "BgituGrades.dll"]