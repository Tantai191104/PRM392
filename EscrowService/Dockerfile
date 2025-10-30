FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5141

RUN mkdir -p /app/logs

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["EscrowService/EscrowService.csproj", "EscrowService/"]
COPY ["SharedKernel/SharedKernel.csproj", "SharedKernel/"]
RUN dotnet restore "EscrowService/EscrowService.csproj"

# ✅ Chỉ copy 2 folder cần thiết
COPY EscrowService/ EscrowService/
COPY SharedKernel/ SharedKernel/

WORKDIR "/src/EscrowService"
RUN dotnet build "EscrowService.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /src/EscrowService
RUN dotnet publish "./EscrowService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EscrowService.dll"]
