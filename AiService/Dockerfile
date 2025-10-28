FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5143

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AiService/AiService.csproj", "AiService/"]
RUN dotnet restore "AiService/AiService.csproj"
COPY . .
WORKDIR "/src/AiService"
RUN dotnet build "AiService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AiService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AiService.dll"]
