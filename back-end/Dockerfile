FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./AdsApi.csproj .
RUN dotnet restore ./AdsApi.csproj
COPY . .

# If repo contains Data/ads.json but not Data/products.json, create products.json for compatibility
RUN if [ -d Data ] && [ -f Data/ads.json ] && [ ! -f Data/products.json ]; then cp Data/ads.json Data/products.json; fi

RUN dotnet publish ./AdsApi.csproj -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
# Copy data files into the runtime image so the app can read initial snapshot
COPY --from=build /src/Data /app/Data
RUN mkdir -p /app/wwwroot/uploads/large && mkdir -p /app/wwwroot/uploads/thumbs
EXPOSE 8080
ENTRYPOINT ["dotnet", "AdsApi.dll"]
