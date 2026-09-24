# Build context
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY localmarket-preordersystem.Domain/*.csproj         localmarket-preordersystem.Domain/
COPY localmarket-preordersystem.Application/*.csproj    localmarket-preordersystem.Application/
COPY localmarket-preordersystem.Infrastructure/*.csproj localmarket-preordersystem.Infrastructure/
COPY localmarket-preordersystem.api/*.csproj            localmarket-preordersystem.api/
RUN dotnet restore localmarket-preordersystem.api/localmarket-preordersystem.api.csproj

COPY . .
RUN dotnet publish localmarket-preordersystem.api/localmarket-preordersystem.api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "localmarket-preordersystem.api.dll"]
