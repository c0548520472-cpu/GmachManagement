# Build from the repository root so all four projects are available.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
# Data is loaded via reflection, so restore it explicitly before API publish.
RUN dotnet restore GmachManagement.Data/GmachManagement.Data.csproj
RUN dotnet restore GmachManagement.API/GmachManagement.API.csproj
RUN dotnet publish GmachManagement.API/GmachManagement.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "GmachManagement.API.dll"]
