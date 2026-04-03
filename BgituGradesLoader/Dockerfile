FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BgituGradesLoader.csproj", "."]
RUN dotnet restore "./BgituGradesLoader.csproj"
COPY . .
RUN dotnet publish "./BgituGradesLoader.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o /app/publish

FROM scratch
COPY --from=build /app/publish /app/publish