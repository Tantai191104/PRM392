FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5142

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["ChatService/ChatService.csproj", "ChatService/"]
COPY ["SharedKernel/SharedKernel.csproj", "SharedKernel/"]
RUN dotnet restore "ChatService/ChatService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/ChatService"
RUN dotnet build "ChatService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatService.dll"]

