FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution file
COPY ./Triumph.HealthMS.slnx ./

# Copy only the required project files (excludes tests and Aspire AppHost)
COPY ./src/core/Triumph.HealthMS.Domain/Triumph.HealthMS.Domain.csproj ./src/core/Triumph.HealthMS.Domain/
COPY ./src/core/Triumph.HealthMS.Application/Triumph.HealthMS.Application.csproj ./src/core/Triumph.HealthMS.Application/
COPY ./src/core/Triumph.HealthMS.Shared/Triumph.HealthMS.Shared.csproj ./src/core/Triumph.HealthMS.Shared/
COPY ./src/infrastructure/Triumph.HealthMS.Persistence/Triumph.HealthMS.Persistence.csproj ./src/infrastructure/Triumph.HealthMS.Persistence/
COPY ./src/infrastructure/Triumph.HealthMS.ExternalServices/Triumph.HealthMS.ExternalServices.csproj ./src/infrastructure/Triumph.HealthMS.ExternalServices/
COPY ./src/presentation/Triumph.HealthMS.Commands/Triumph.HealthMS.Commands.csproj ./src/presentation/Triumph.HealthMS.Commands/
COPY ./src/presentation/Triumph.HealthMS.Queries/Triumph.HealthMS.Queries.csproj ./src/presentation/Triumph.HealthMS.Queries/
COPY ./src/presentation/Triumph.HealthMS.Host/Triumph.HealthMS.Host.csproj ./src/presentation/Triumph.HealthMS.Host/

# Restore NuGet packages for the Host project (which will restore all dependencies)
RUN dotnet restore "src/presentation/Triumph.HealthMS.Host/Triumph.HealthMS.Host.csproj"

# Copy source code
COPY ./src ./src

# Build the API host project
RUN dotnet build "src/presentation/Triumph.HealthMS.Host/Triumph.HealthMS.Host.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/presentation/Triumph.HealthMS.Host/Triumph.HealthMS.Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Triumph.HealthMS.Host.dll"]
