# Multi-stage Dockerfile for ASP.NET Core 9.0 Web App
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for caching nuget restore
COPY ["ChessCore/ChessCore.csproj", "ChessCore/"]
RUN dotnet restore "ChessCore/ChessCore.csproj"

# Copy the entire source code and build
COPY . .
WORKDIR "/src/ChessCore"
RUN dotnet publish "ChessCore.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Expose HTTP port 8080 (ASP.NET Core 9 default in containers)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ChessCore.dll"]
