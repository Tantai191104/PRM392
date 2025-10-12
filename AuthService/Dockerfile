FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy entire repo (build context set to repo root in docker-compose)
COPY . .

# Restore the full solution so shared projects and packages are resolved
RUN dotnet restore "PRM392.sln"

# publish the service project
RUN dotnet publish "AuthService/AuthService.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5133
EXPOSE 5133
ENTRYPOINT ["dotnet", "AuthService.dll"]
