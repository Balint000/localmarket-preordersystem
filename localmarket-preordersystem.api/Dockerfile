# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["localmarket-preordersystem.api/localmarket-preordersystem.api.csproj", "localmarket-preordersystem.api/"]
COPY ["localmarket-preordersystem.Application/localmarket-preordersystem.Application.csproj", "localmarket-preordersystem.Application/"]
COPY ["localmarket-preordersystem.Domain/localmarket-preordersystem.Domain.csproj", "localmarket-preordersystem.Domain/"]
COPY ["localmarket-preordersystem.Infrastructure/localmarket-preordersystem.Infrastructure.csproj", "localmarket-preordersystem.Infrastructure/"]
RUN dotnet restore "./localmarket-preordersystem.api/localmarket-preordersystem.api.csproj"
COPY . .
WORKDIR "/src/localmarket-preordersystem.api"
RUN dotnet build "./localmarket-preordersystem.api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./localmarket-preordersystem.api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# a feltöltött képek mappája (volume-ként csatolva); a nem-root felhasználónak írhatónak kell lennie
USER root
RUN mkdir -p /app/uploads/products && chown -R $APP_UID /app/uploads
USER $APP_UID
ENTRYPOINT ["dotnet", "localmarket-preordersystem.api.dll"]