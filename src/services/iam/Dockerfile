# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared projects
COPY src/shared/building-blocks/Platform.BuildingBlocks.csproj shared/building-blocks/
COPY src/shared/contracts/Platform.Contracts.csproj shared/contracts/
COPY src/shared/security/Platform.Security.csproj shared/security/

# Copy IAM service projects
COPY src/services/iam/src/IAM.Domain/IAM.Domain.csproj services/iam/src/IAM.Domain/
COPY src/services/iam/src/IAM.Application/IAM.Application.csproj services/iam/src/IAM.Application/
COPY src/services/iam/src/IAM.Infrastructure/IAM.Infrastructure.csproj services/iam/src/IAM.Infrastructure/
COPY src/services/iam/src/IAM.API/IAM.API.csproj services/iam/src/IAM.API/

# Restore dependencies
RUN dotnet restore services/iam/src/IAM.API/IAM.API.csproj

# Copy source code
COPY src/shared/ shared/
COPY src/services/iam/ services/iam/

# Build
WORKDIR /src/services/iam/src/IAM.API
RUN dotnet build -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "IAM.API.dll"]
