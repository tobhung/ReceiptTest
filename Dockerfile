# 1. 構建階段 (使用 SDK 映像檔)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 複製 csproj 並還原套件 (利用快取優化速度)
COPY *.sln .
COPY *.csproj .
RUN dotnet restore

# 複製所有原始碼並編譯
COPY . .
# 關鍵：針對樹莓派 ARM64 架構進行發佈
RUN dotnet publish -c Release -o /out -r linux-arm64 --self-contained false

# 2. 執行階段 (使用輕量化 ARM64 Runtime 映像檔)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim-arm64v8
WORKDIR /app
COPY --from=build /out .

# 設定環境變數（視需求調整）
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "ReceiptTest.dll"]