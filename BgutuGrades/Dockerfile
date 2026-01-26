FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BgutuGrades.csproj", "./"]
RUN dotnet restore "./BgutuGrades.csproj"
COPY . .
RUN dotnet build "./BgutuGrades.csproj" -c $BUILD_CONFIGURATION -o /src/build

FROM build AS publish
RUN dotnet publish "./BgutuGrades.csproj" -c Release -o /src/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /src/publish .
COPY ["appsettings.Production.json", "./"]
ENTRYPOINT ["dotnet", "BgutuGrades.dll"]