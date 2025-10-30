# =========================
# Stage 1: Build & Publish
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy các file cần thiết
COPY APIGateway/APIGateway.csproj APIGateway/
COPY SharedKernel/SharedKernel.csproj SharedKernel/

# Restore dependencies chỉ cho APIGateway
RUN dotnet restore APIGateway/APIGateway.csproj

# Copy toàn bộ source code sau khi restore xong
COPY . .

# Set working directory tới project chính
WORKDIR /src/APIGateway

# Build & publish app ra thư mục /app/publish
RUN dotnet publish -c Release -o /app/publish --no-restore

# =========================
# Stage 2: Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5016
EXPOSE 5016

ENTRYPOINT ["dotnet", "APIGateway.dll"]
