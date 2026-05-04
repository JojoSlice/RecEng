  FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
  WORKDIR /src
  COPY recommendation-service/recommendation-service.csproj recommendation-service/
  RUN dotnet restore recommendation-service/recommendation-service.csproj
  COPY recommendation-service/ recommendation-service/
  RUN dotnet publish recommendation-service/recommendation-service.csproj -c Release -o /app/publish

  FROM mcr.microsoft.com/dotnet/aspnet:10.0
  WORKDIR /app
  COPY --from=build /app/publish .
  ENTRYPOINT ["dotnet", "recommendation-service.dll"]
