# Определяем пути
$sourcePath = "E:\bobgroup\projects\mediaOrcestrator\repo\MediaOrcestrator\MediaOrcestrator.Runner\bin\Debug\net8.0-windows"
$moduleBuildsPath = "E:\bobgroup\projects\mediaOrcestrator\repo\MediaOrcestrator\ModuleBuilds"
$targetPath = "E:\bobgroup\projects\mediaOrcestrator\Production\App"
$backupBasePath = "E:\bobgroup\projects\mediaOrcestrator\Production\App_backup"

# Функция для получения следующего номера бэкапа (максимальный + 1)
function Get-NextBackupNumber {
    param($backupPath)
    
    if (Test-Path $backupPath) {
        $existingBackups = Get-ChildItem -Path $backupPath -Directory | 
                           Where-Object { $_.Name -match '^\d+$' } | 
                           ForEach-Object { [int]$_.Name }
        
        if ($existingBackups.Count -gt 0) {
            return ($existingBackups | Measure-Object -Maximum).Maximum + 1
        }
    }
    return 1
}

# Получаем номер для бэкапа
$backupNumber = Get-NextBackupNumber -backupPath $backupBasePath
$backupPath = Join-Path $backupBasePath $backupNumber.ToString()

Write-Host "Номер бэкапа: $backupNumber" -ForegroundColor Yellow
Write-Host "Путь для бэкапа: $backupPath" -ForegroundColor Yellow

# 1. Создаём бэкап текущего содержимого папки App (исключая logs и tools) и сразу удаляем
if (Test-Path $targetPath) {
    Write-Host "Создание бэкапа из $targetPath..." -ForegroundColor Cyan
    
    # Создаём папку для бэкапа
    New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
    
    # Получаем все элементы в папке App, исключая logs и tools
    $itemsToBackup = Get-ChildItem -Path $targetPath | 
                     Where-Object { $_.Name -notin @('logs', 'tools', 'settings.txt') }
    
    # Перемещаем каждый элемент в бэкап
    foreach ($item in $itemsToBackup) {
        $destination = Join-Path $backupPath $item.Name

        if ($item.Name -eq 'settings.txt') {
            #Write-Host "  Копирование файла: $($item.Name) (оставляем в исходной папке)" -ForegroundColor Gray
            Copy-Item -Path $item.FullName -Destination $destination -Force
        } elseif ($item.PSIsContainer) {
            #Write-Host "  Перемещение папки: $($item.Name)" -ForegroundColor Gray
            Move-Item -Path $item.FullName -Destination $destination -Force
        } else {
            #Write-Host "  Перемещение файла: $($item.Name)" -ForegroundColor Gray
            Move-Item -Path $item.FullName -Destination $destination -Force
        }
    }
    
    Write-Host "Бэкап создан в: $backupPath" -ForegroundColor Green
} else {
    Write-Host "Папка назначения не существует, создаём новую..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
}

# 2. Копируем новые файлы из источника в папку App (исключая logs, tools и settings.txt)
Write-Host "`nКопирование новых файлов из $sourcePath..." -ForegroundColor Cyan

# Проверяем существование исходной папки
if (-not (Test-Path $sourcePath)) {
    Write-Host "Ошибка: Исходная папка не найдена: $sourcePath" -ForegroundColor Red
    exit 1
}

# Получаем все элементы из источника, исключая logs, tools и settings.txt
$itemsToCopy = Get-ChildItem -Path $sourcePath | 
               Where-Object { $_.Name -notin @('logs', 'tools', 'settings.txt') }

foreach ($item in $itemsToCopy) {
    $destination = Join-Path $targetPath $item.Name
    if ($item.PSIsContainer) {
        #Write-Host "  Копирование папки: $($item.Name)" -ForegroundColor Gray
        Copy-Item -Path $item.FullName -Destination $destination -Recurse -Force
    } else {
        #Write-Host "  Копирование файла: $($item.Name)" -ForegroundColor Gray
        Copy-Item -Path $item.FullName -Destination $destination -Force
    }
}

# 3. Копируем ModuleBuilds в папку App
Write-Host "`nКопирование ModuleBuilds из $moduleBuildsPath..." -ForegroundColor Cyan

# Проверяем существование папки ModuleBuilds
if (Test-Path $moduleBuildsPath) {
    $moduleBuildsDestination = Join-Path $targetPath "ModuleBuilds"
    
    # Если папка ModuleBuilds уже существует, удаляем её перед копированием
    if (Test-Path $moduleBuildsDestination) {
        Write-Host "  Удаление существующей папки ModuleBuilds..." -ForegroundColor Gray
        Remove-Item -Path $moduleBuildsDestination -Recurse -Force
    }
    
    Write-Host "  Копирование папки ModuleBuilds..." -ForegroundColor Gray
    Copy-Item -Path $moduleBuildsPath -Destination $moduleBuildsDestination -Recurse -Force
    
    Write-Host "ModuleBuilds скопирована в: $moduleBuildsDestination" -ForegroundColor Green
} else {
    Write-Host "Предупреждение: Папка ModuleBuilds не найдена: $moduleBuildsPath" -ForegroundColor Yellow
}

Write-Host "`nГотово!" -ForegroundColor Green
Write-Host "Все файлы скопированы в: $targetPath" -ForegroundColor Green
Write-Host "Бэкап создан в: $backupPath" -ForegroundColor Green