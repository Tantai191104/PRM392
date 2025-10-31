# =============================
# Stage 1: Base runtime
# =============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5141

# Tạo thư mục logs
RUN mkdir -p /app/logs

# =============================
# Stage 2: Build
# =============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Chỉ copy các file csproj cần thiết để restore
COPY ["EscrowService/EscrowService.csproj", "EscrowService/"]
COPY ["SharedKernel/SharedKernel.csproj", "SharedKernel/"]

# Restore các dependencies
RUN dotnet restore "EscrowService/EscrowService.csproj"

# Copy source code
COPY EscrowService/ EscrowService/
COPY SharedKernel/ SharedKernel/

WORKDIR "/src/EscrowService"

# Build project
RUN dotnet build "EscrowService.csproj" -c Release -o /app/build

# =============================
# Stage 3: Publish
# =============================
FROM build AS publish
WORKDIR "/src/EscrowService"

RUN dotnet publish "EscrowService.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =============================
# Stage 4: Final image
# =============================
FROM base AS final
WORKDIR /app

# Copy ứng dụng đã publish từ stage trước
COPY --from=publish /app/publish .

# ENTRYPOINT
ENTRYPOINT ["dotnet", "EscrowService.dll"]
