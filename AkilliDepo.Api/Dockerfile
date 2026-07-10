# Render (ve benzeri Docker tabanlı platformlar) için çok aşamalı derleme.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY AkilliDepo.Api.csproj .
RUN dotnet restore "AkilliDepo.Api.csproj"
COPY . .
RUN dotnet publish "AkilliDepo.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render, PORT ortam değişkenini kendisi atar (varsayılan 10000); Kestrel o porttan dinlemeli.
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:$PORT dotnet AkilliDepo.Api.dll"]
