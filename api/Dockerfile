FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY RecEng.Contracts/RecEng.Contracts.csproj RecEng.Contracts/
COPY api/api.csproj api/
RUN dotnet restore api/api.csproj
COPY RecEng.Contracts/ RecEng.Contracts/
COPY api/ api/
RUN dotnet publish api/api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "api.dll"]
