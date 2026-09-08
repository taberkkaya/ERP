FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ERPServer.Domain/ ERPServer.Domain/
COPY ERPServer.Application/ ERPServer.Application/
COPY ERPServer.Infrastructure/ ERPServer.Infrastructure/
COPY ERPServer.WebAPI/ ERPServer.WebAPI/
COPY nuget.config .

RUN dotnet publish ERPServer.WebAPI/ERPServer.WebAPI.csproj \
    -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# curl yalnizca saglik kontrolu icin; Coolify yeni konteynere ne zaman
# gececegini bu uctan anliyor.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "ERPServer.WebAPI.dll"]
