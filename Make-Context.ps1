param(
  # Корни проектов (относительно текущего каталога)
  [string]$BackendDir = ".\Server",
  [string]$FrontendDir = ".\Client",

  # Куда класть результат
  [string]$OutDir = ".\context-dump"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# ===== Хелперы совместимости =====

function Get-RelativePath {
  param(
    [Parameter(Mandatory=$true)][string]$BasePath,
    [Parameter(Mandatory=$true)][string]$FullPath
  )
  
  # Используем встроенный метод .NET для относительных путей (доступен в .NET 4.0+)
  $base = [System.IO.Path]::GetFullPath($BasePath)
  $full = [System.IO.Path]::GetFullPath($FullPath)
  
  try {
    $relativePath = [System.IO.Path]::GetRelativePath($base, $full)
    return $relativePath
  } catch {
    # Если метод не доступен (старая версия .NET), используем обходной путь
    if ($full.StartsWith($base)) {
      return $full.Substring($base.Length).TrimStart('\', '/')
    } else {
      return $full
    }
  }
}

# ===== Маски для включения =====
$CodeExts = @(
  # .NET / C#
  "*.sln","*.csproj","*.props","*.targets","*.cs",
  # Web / Frontend
  "*.ts","*.tsx","*.js","*.jsx",
  # Общие конфиги/скрипты/доки
  "*.json","*.yml","*.yaml","*.md","*.toml","*.env.example",
  "*.ps1","*.psm1","*.bat","*.cmd","*.sh",
  "Dockerfile","docker-compose.*",
  # SQL и миграции
  "*.sql","*.xml","*.proto"
)

# ===== Исключаемые каталоги =====
$ExcludeDirs = @(
  "\.git\", "\.github\", "\.gitlab\",
  "\.idea\", "\.vscode\",
  "\node_modules\", "\.next\", "\.turbo\", "\dist\", "\out\",
  "\bin\", "\obj\", "\.vs\", "\TestResults\", "\coverage\", "\.nyc_output\",
  "\.terraform\", "\terraform.tfstate.d\",
  "\.venv\", "\__pycache__"
)

# ===== Исключаемые файлы (секреты) =====
$ExcludeFiles = @(
  ".env", ".env.*", "*.secret*", "*secrets*.json",
  "*.pfx","*.pem","*.key","*.cer","*.crt"
)

function Test-ExcludedPath {
  param([string]$FullPath)
  foreach($ed in $ExcludeDirs){
    if ($FullPath -like "*$ed*"){ return $true }
  }
  $name = [IO.Path]::GetFileName($FullPath)
  foreach($ef in $ExcludeFiles){
    if ($name -like $ef){ return $true }
  }
  return $false
}

function Test-IncludedByExt {
  param([string]$FullPath)
  $name = [IO.Path]::GetFileName($FullPath)
  foreach($pat in $CodeExts){
    if ($pat -eq "Dockerfile"){
      if ($name -eq "Dockerfile"){ return $true }
    } elseif ($pat -like "docker-compose.*") {
      if ($name -like "docker-compose.*"){ return $true }
    } elseif ($name -like $pat){
      return $true
    }
  }
  return $false
}

function Get-CodeFiles {
  param([string]$Root)

  if (-not (Test-Path $Root)) {
    Write-Warning "Путь не найден: $Root"
    return @()
  }

  if (Test-Path (Join-Path $Root ".git")) {
    Push-Location $Root
    try {
      $files = git ls-files | ForEach-Object { Join-Path $Root $_ }
    } catch {
      $files = Get-ChildItem -Path $Root -Recurse -File | Select-Object -ExpandProperty FullName
    } finally {
      Pop-Location
    }
  } else {
    $files = Get-ChildItem -Path $Root -Recurse -File | Select-Object -ExpandProperty FullName
  }

  $files = $files |
    Where-Object { -not (Test-ExcludedPath $_) } |
    Where-Object { Test-IncludedByExt $_ } |
    Sort-Object
  return $files
}

function Write-Context {
  param(
    [string]$Root,
    [string]$OutTxt
  )

  if (Test-Path $OutTxt) { Remove-Item $OutTxt -Force }

  "# Context dump generated: $([DateTime]::UtcNow.ToString('u')) (UTC)" | Out-File $OutTxt -Encoding UTF8
  "# Root: $((Resolve-Path $Root).Path)" | Out-File $OutTxt -Append -Encoding UTF8
  "" | Out-File $OutTxt -Append -Encoding UTF8

  $rootAbs = (Resolve-Path $Root).Path
  $files = Get-CodeFiles -Root $Root
  foreach($f in $files) {
    $rel = Get-RelativePath -BasePath $rootAbs -FullPath $f
    "================================================================" | Out-File $OutTxt -Append -Encoding UTF8
    "FILE: $rel" | Out-File $OutTxt -Append -Encoding UTF8
    "----------------------------------------------------------------" | Out-File $OutTxt -Append -Encoding UTF8
    try {
      Get-Content -Path $f -Raw -Encoding UTF8 | Out-File $OutTxt -Append -Encoding UTF8
    } catch {
      try {
        Get-Content -Path $f -Raw -Encoding Default | Out-File $OutTxt -Append -Encoding UTF8
      } catch {
        "!! [SKIPPED: cannot read file due to encoding]" | Out-File $OutTxt -Append -Encoding UTF8
      }
    }
    "" | Out-File $OutTxt -Append -Encoding UTF8
  }
}

$backOut = Join-Path $OutDir "backend-context.txt"
$frontOut = Join-Path $OutDir "frontend-context.txt"

Write-Context -Root $BackendDir -OutTxt $backOut
Write-Context -Root $FrontendDir -OutTxt $frontOut

Write-Host "✔ Готово. Результаты:"
Write-Host "  $backOut"
Write-Host "  $frontOut"