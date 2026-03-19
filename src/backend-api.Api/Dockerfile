FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything and build
COPY . ./
RUN dotnet publish -c Release -o /out

# ── Runtime image ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create logs directory for Serilog file sink
RUN mkdir -p /app/logs

COPY --from=build /out ./

# Expose HTTP port (Nginx will front this)
EXPOSE 8080

ENTRYPOINT ["dotnet", "backend-api.Api.dll"]
