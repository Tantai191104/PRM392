FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy entire repo (build context set to repo root in docker-compose)
COPY . .

# Restore solution to ensure all packages are available
RUN dotnet restore "PRM392.sln"

# publish the specific project
RUN dotnet publish "ProductService/ProductService.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5137
EXPOSE 5137
ENTRYPOINT ["dotnet", "ProductService.dll"]
