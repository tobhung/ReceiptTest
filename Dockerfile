# 1. 構建階段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.sln .
COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /out -r linux-arm64 --self-contained false

# 2. 執行階段
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim-arm64v8
WORKDIR /app
COPY --from=build /out .

# --- 加入這段：安裝 lp 工具 ---
RUN apt-get update && \
    apt-get install -y --no-install-recommends cups-client && \
    rm -rf /var/lib/apt/lists/*
# -----------------------------

COPY --from=build /out .

# 告訴 Watchtower：請自動更新這個容器
LABEL com.centurylinklabs.watchtower.enable="true"

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "ReceiptTest.dll"]