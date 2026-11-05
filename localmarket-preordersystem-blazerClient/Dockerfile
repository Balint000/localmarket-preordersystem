FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY localmarket-preordersystem-blazerClient/*.csproj localmarket-preordersystem-blazerClient/
RUN dotnet restore localmarket-preordersystem-blazerClient/localmarket-preordersystem-blazerClient.csproj

COPY localmarket-preordersystem-blazerClient/ localmarket-preordersystem-blazerClient/
RUN dotnet publish localmarket-preordersystem-blazerClient/localmarket-preordersystem-blazerClient.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "localmarket-preordersystem-blazerClient.dll"]
