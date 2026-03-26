FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY RecEng.Contracts/RecEng.Contracts.csproj RecEng.Contracts/
COPY analytics-service/analytics-service.csproj analytics-service/
RUN dotnet restore analytics-service/analytics-service.csproj
COPY RecEng.Contracts/ RecEng.Contracts/
COPY analytics-service/ analytics-service/
RUN dotnet publish analytics-service/analytics-service.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "analytics-service.dll"]
