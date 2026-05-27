FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY services/ ./services/
WORKDIR /src/services/core-api
RUN dotnet publish DevPulse.Api.csproj -c Release -o /app/publish
RUN dotnet tool install --global Microsoft.Playwright.CLI \
    && ~/.dotnet/tools/playwright install chromium --with-deps

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright
RUN apt-get update && apt-get install -y \
    ffmpeg \
    libnss3 libatk1.0-0 libatk-bridge2.0-0 libcups2 \
    libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 \
    libxrandr2 libgbm1 libasound2 \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "DevPulse.Api.dll"]
