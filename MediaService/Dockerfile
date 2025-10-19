FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5140

# Create uploads directory
RUN mkdir -p /app/uploads

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["MediaService/MediaService.csproj", "MediaService/"]
COPY ["SharedKernel/SharedKernel.csproj", "SharedKernel/"]
RUN dotnet restore "MediaService/MediaService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/MediaService"
RUN dotnet build "MediaService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MediaService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MediaService.dll"]

