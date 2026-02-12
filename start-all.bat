@echo off
echo 🥛 Starting Milk API Manager System for Windows...

:: 1. Start Infrastructure (APISIX, Etcd, ELK, Monitoring)
echo 🚀 Launching infrastructure containers...
docker-compose up -d

:: 2. Wait for APISIX to be ready (use admin API port 9180)
echo ⏳ Waiting for APISIX Admin API (http://localhost:9180)...
:wait_apisix
curl --fail -s http://localhost:9180/apisix/admin/routes -H "X-API-KEY: edd1c9f034335f136f87ad84b625c88b" >nul 2>&1
if %errorlevel% neq 0 (
    echo   APISIX not ready yet, retrying in 2s...
    timeout /t 2 >nul
    goto wait_apisix
)
echo ✅ APISIX Admin API is reachable.

:: 3. Setup Initial Seed Data
echo 🌱 Injecting seed data...
:: Note: Assuming curl is available in Win11 (it usually is)
:: Running the shell script logic via curl commands if needed, 
:: but for MVP we assume manual or existing etcd state.

:: 4. Start Backend (MilkApiManager)
:: 4. Ensure dotnet exists
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ⚠️ dotnet CLI not found in PATH. Please install .NET SDK and try again.
    exit /b 1
)

echo ⚙️ Starting Backend API (MilkApiManager)...
start "MilkApiManager" cmd /c "cd /d %~dp0backend\MilkApiManager && dotnet run --project MilkApiManager.csproj"

:: 5. Start Blazor UI (MilkAdminBlazor)
echo 🖥️ Starting Admin Dashboard (MilkAdminBlazor)...
start "MilkAdminBlazor" cmd /c "cd /d %~dp0backend\MilkAdminBlazor && dotnet run --project MilkAdminBlazor.csproj"

echo ✅ System is up!
echo -----------------------------------
echo APISIX Gateway: http://localhost:9080
echo APISIX Dashboard: http://localhost:9000
echo Milk Admin UI: http://localhost:5000
echo Grafana: http://localhost:3000
echo Kibana: http://localhost:5601
echo -----------------------------------
echo Press Ctrl+C in this window to stop (Note: containers will remain running).
exit /b 0
