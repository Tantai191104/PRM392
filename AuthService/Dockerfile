# =========================
# Stage 1: Build & Publish
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Chỉ copy những file cần thiết
COPY AuthService/AuthService.csproj AuthService/
COPY SharedKernel/SharedKernel.csproj SharedKernel/

# Restore dependencies cho đúng project
RUN dotnet restore AuthService/AuthService.csproj

# Copy toàn bộ source code (nhưng không lồng thư mục)
COPY AuthService/. AuthService/
COPY SharedKernel/. SharedKernel/

WORKDIR /src/AuthService

# Build & publish app ra thư mục /app/publish
RUN dotnet publish -c Release -o /app/publish --no-restore

# =========================
# Stage 2: Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5133
EXPOSE 5133

ENTRYPOINT ["dotnet", "AuthService.dll"]
