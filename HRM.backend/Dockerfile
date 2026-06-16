# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY HRM.backend/HRM.backend.csproj HRM.backend/
RUN dotnet restore HRM.backend/HRM.backend.csproj

FROM restore AS build
COPY HRM.backend/ HRM.backend/
WORKDIR /src/HRM.backend
RUN dotnet publish HRM.backend.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

RUN mkdir -p /app/wwwroot /app/Logs
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "HRM.backend.dll"]
