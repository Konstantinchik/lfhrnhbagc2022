# Автозапуск MCP Integration для Unity
# Запускает MCP Server и готовит окружение для работы с Claude

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MCP Integration для DarkTree FPS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Проверка Node.js
Write-Host "[1/4] Проверка Node.js..." -ForegroundColor Yellow
$nodeVersion = node --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Node.js не установлен!" -ForegroundColor Red
    Write-Host "Установите Node.js >= 18.0.0 с https://nodejs.org" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Node.js $nodeVersion установлен" -ForegroundColor Green
Write-Host ""

# Проверка зависимостей MCP Server
Write-Host "[2/4] Проверка зависимостей..." -ForegroundColor Yellow
$mcpServerPath = "$PSScriptRoot\mcp-server"
if (-not (Test-Path "$mcpServerPath\node_modules")) {
    Write-Host "Зависимости не установлены. Устанавливаю..." -ForegroundColor Yellow
    Push-Location $mcpServerPath
    npm install
    Pop-Location
}
Write-Host "✓ Зависимости установлены" -ForegroundColor Green
Write-Host ""

# Проверка .env файла
Write-Host "[3/4] Проверка конфигурации..." -ForegroundColor Yellow
if (-not (Test-Path "$mcpServerPath\.env")) {
    Write-Host ".env файл не найден. Создаю из .env.example..." -ForegroundColor Yellow
    Copy-Item "$mcpServerPath\.env.example" "$mcpServerPath\.env"
}
Write-Host "✓ Конфигурация готова" -ForegroundColor Green
Write-Host ""

# Запуск MCP Server
Write-Host "[4/4] Запуск MCP Server..." -ForegroundColor Yellow
Write-Host ""
Write-Host "MCP Server запускается в новом окне..." -ForegroundColor Cyan
Write-Host ""

Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$mcpServerPath'; Write-Host 'MCP Server для Unity' -ForegroundColor Green; Write-Host ''; npm run dev"
)

Start-Sleep -Seconds 2

Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✓ MCP Integration запущен!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Следующие шаги:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Откройте Unity Editor (2022.3.15f1)" -ForegroundColor White
Write-Host "   → MCP Bridge запустится автоматически" -ForegroundColor Gray
Write-Host ""
Write-Host "2. В Unity Console должно появиться:" -ForegroundColor White
Write-Host "   [MCPBridge] Server started on port 7777" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Запустите Claude Code в этой директории" -ForegroundColor White
Write-Host "   → MCP инструменты будут доступны автоматически" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Протестируйте интеграцию:" -ForegroundColor White
Write-Host "   'Check Unity connection and create test cube'" -ForegroundColor Gray
Write-Host ""
Write-Host "Документация:" -ForegroundColor Cyan
Write-Host "  • QUICKSTART.md     - Быстрый старт за 5 минут" -ForegroundColor White
Write-Host "  • MCP_API.md        - Все доступные команды" -ForegroundColor White
Write-Host "  • MCP_SETUP.md      - Детальная настройка" -ForegroundColor White
Write-Host ""
Write-Host "Порты:" -ForegroundColor Cyan
Write-Host "  • Unity Bridge: localhost:7777" -ForegroundColor White
Write-Host "  • MCP Server: stdio (автоматически)" -ForegroundColor White
Write-Host ""
Write-Host "Для остановки: закройте окно MCP Server" -ForegroundColor Yellow
Write-Host ""
