# Script para automatizar pruebas E2E con Docker y Bruno

Write-Host "🛑 Deteniendo y eliminando contenedores antiguos..." -ForegroundColor Yellow
docker compose down -v

Write-Host "🏗️  Construyendo y levantando contenedores..." -ForegroundColor Cyan
docker compose up --build -d

Write-Host "⏳ Esperando a que los servicios estén listos (15 segundos)..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host "🚀 Ejecutando pruebas E2E con Bruno..." -ForegroundColor Green

# Cambiamos al directorio de la colección para asegurar que Bruno encuentre el bruno.json
Push-Location "BrunoTest/teste2e/collections/tests"

try {
    # Ejecutamos bruno indicando la ruta actual (.)
    # El reporte se guarda en ../../results.html para que quede en la raíz del proyecto
    $command = "npx @usebruno/cli run . -r --env Local --insecure --reporter-html ../../results.html"
    Invoke-Expression $command
}
finally {
    # Volvemos al directorio original pase lo que pase
    Pop-Location
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Pruebas completadas exitosamente." -ForegroundColor Green
} else {
    Write-Host "❌ Hubo fallos en las pruebas." -ForegroundColor Red
}

Write-Host "📄 Reporte generado en: results.html" -ForegroundColor Cyan
