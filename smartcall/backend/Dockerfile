# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SmartCall.sln .
COPY src/SmartCall.Domain/SmartCall.Domain.csproj src/SmartCall.Domain/
COPY src/SmartCall.Application/SmartCall.Application.csproj src/SmartCall.Application/
COPY src/SmartCall.Infrastructure/SmartCall.Infrastructure.csproj src/SmartCall.Infrastructure/
COPY src/SmartCall.Api/SmartCall.Api.csproj src/SmartCall.Api/
RUN dotnet restore src/SmartCall.Api/SmartCall.Api.csproj

COPY src/ src/
RUN dotnet publish src/SmartCall.Api/SmartCall.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Recordings / uploads live here; mount a volume in production.
RUN mkdir -p /app/storage
VOLUME ["/app/storage"]

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SmartCall.Api.dll"]
