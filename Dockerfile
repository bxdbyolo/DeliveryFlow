# этап 1: сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY DeliveryFlow.Api/*.csproj ./DeliveryFlow.Api/

RUN dotnet restore ./DeliveryFlow.Api/DeliveryFlow.Api.csproj

COPY DeliveryFlow.Api/. ./DeliveryFlow.Api/

RUN dotnet publish ./DeliveryFlow.Api/DeliveryFlow.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# этап 2: запуск приложения
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "DeliveryFlow.Api.dll"]
