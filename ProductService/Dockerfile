FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ProductService/ProductService.csproj ProductService/
RUN dotnet restore "ProductService/ProductService.csproj"
COPY . .
WORKDIR /src/ProductService
RUN dotnet build "ProductService.csproj" -c Release -o /app/build
RUN dotnet publish "ProductService.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5137
EXPOSE 5137

ENTRYPOINT ["dotnet", "ProductService.dll"]
