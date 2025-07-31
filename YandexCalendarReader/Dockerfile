FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["YandexCalendarReader/YandexCalendarReader.csproj", "YandexCalendarReader/"]
RUN dotnet restore "YandexCalendarReader/YandexCalendarReader.csproj"
COPY . .
WORKDIR "/src/YandexCalendarReader"
RUN dotnet build "./YandexCalendarReader.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./YandexCalendarReader.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YandexCalendarReader.dll"]
