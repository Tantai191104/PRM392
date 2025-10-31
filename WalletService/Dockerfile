# Stage 1: Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5150

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj để restore trước
COPY WalletService/WalletService.csproj ./WalletService/
WORKDIR /src/WalletService

# Restore package (bao gồm Newtonsoft.Json)
RUN dotnet restore "WalletService.csproj"

# Copy toàn bộ source
COPY WalletService/ ./

# Build project
RUN dotnet build "WalletService.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "WalletService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WalletService.dll"]
