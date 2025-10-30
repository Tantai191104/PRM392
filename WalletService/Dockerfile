FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5150
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY WalletService/WalletService.csproj WalletService/
RUN dotnet restore "WalletService/WalletService.csproj"

COPY . .

# Build app
WORKDIR /src/WalletService
RUN dotnet build "WalletService.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "WalletService.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app

# Copy từ stage publish
COPY --from=publish /app/publish .

# Chạy ứng dụng
ENTRYPOINT ["dotnet", "WalletService.dll"]
