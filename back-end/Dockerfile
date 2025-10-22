FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./AdsApi.csproj .
RUN dotnet restore ./AdsApi.csproj
COPY . .
RUN dotnet publish ./AdsApi.csproj -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
RUN mkdir -p /app/Data && mkdir -p /app/wwwroot/uploads/large && mkdir -p /app/wwwroot/uploads/thumbs
EXPOSE 8080
ENTRYPOINT ["dotnet", "AdsApi.dll"]
