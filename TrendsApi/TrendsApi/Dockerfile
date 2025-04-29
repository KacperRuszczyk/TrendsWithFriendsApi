FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY TrendsApi.csproj ./TrendsApi.csproj
RUN dotnet restore TrendsApi.csproj

COPY . .

RUN dotnet publish TrendsApi.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

COPY --from=build /out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "TrendsApi.dll"]

