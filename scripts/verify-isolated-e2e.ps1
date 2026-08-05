[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Net.Http

$projectName = "minio-file-management-final-audit"
$expectedServiceCount = 17
$expectedRunningServiceCount = 14
$expectedOneShotServiceCount = 3
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$composeFile = Join-Path $repoRoot "compose.yaml"
$envFile = Join-Path $repoRoot ".env.example"
$composeArguments = @(
    "compose"
    "--project-name"
    $projectName
    "--file"
    $composeFile
    "--env-file"
    $envFile
)

$environmentNames = @(
    "POSTGRES_DB"
    "POSTGRES_USER"
    "POSTGRES_PASSWORD"
    "POSTGRES_PORT"
    "REDIS_PASSWORD"
    "REDIS_PORT"
    "REDISINSIGHT_PORT"
    "REDISINSIGHT_ENCRYPTION_KEY"
    "MINIO_ROOT_USER"
    "MINIO_ROOT_PASSWORD"
    "MINIO_API_PORT"
    "MINIO_CONSOLE_PORT"
    "API_PORT"
    "IDENTITY_API_PORT"
    "GATEWAY_PORT"
    "REPORTING_PORT"
    "REPORTING_DASHBOARD_USERNAME"
    "REPORTING_DASHBOARD_PASSWORD"
    "KAFKA_PORT"
    "KAFBAT_UI_PORT"
    "KAFBAT_UI_USERNAME"
    "KAFBAT_UI_PASSWORD"
    "WEB_PORT"
    "SEQ_PORT"
    "SEQ_ADMIN_PASSWORD"
    "JWT_SIGNING_KEY"
    "IDENTITY_ADMIN_EMAIL"
    "IDENTITY_ADMIN_PASSWORD"
)

$ports = @{
    POSTGRES_PORT = 15432
    REDIS_PORT = 16379
    REDISINSIGHT_PORT = 15540
    MINIO_API_PORT = 19000
    MINIO_CONSOLE_PORT = 19001
    API_PORT = 15080
    IDENTITY_API_PORT = 15090
    GATEWAY_PORT = 15070
    REPORTING_PORT = 15100
    KAFKA_PORT = 19092
    KAFBAT_UI_PORT = 18085
    WEB_PORT = 18080
    SEQ_PORT = 15341
}

$environmentBackup = @{}
$environmentStarted = $false
$client = [System.Net.Http.HttpClient]::new()

function New-RandomSecret {
    param(
        [int] $ByteCount = 32
    )

    $bytes = New-Object byte[] $ByteCount
    $generator =
        [System.Security.Cryptography.RandomNumberGenerator]::Create()

    try {
        $generator.GetBytes($bytes)
    }
    finally {
        $generator.Dispose()
    }

    $secret =
        [Convert]::ToBase64String($bytes)

    return $secret.Replace(
        "+",
        "-").Replace(
            "/",
            "_").TrimEnd(
                [char]"=")
}

function Set-IsolatedEnvironment {
    foreach ($name in $environmentNames) {
        $environmentBackup[$name] =
            [Environment]::GetEnvironmentVariable(
                $name,
                "Process")
    }

    $values = @{
        POSTGRES_DB = "file_management_final_audit"
        POSTGRES_USER = "final_audit"
        POSTGRES_PASSWORD = New-RandomSecret
        REDIS_PASSWORD = New-RandomSecret
        REDISINSIGHT_ENCRYPTION_KEY =
            New-RandomSecret 48
        MINIO_ROOT_USER = "finalaudit"
        MINIO_ROOT_PASSWORD = New-RandomSecret
        REPORTING_DASHBOARD_USERNAME = "reporting-final-audit"
        REPORTING_DASHBOARD_PASSWORD =
            "Aa1!" + (New-RandomSecret)
        KAFBAT_UI_USERNAME =
            "kafka-final-audit"
        KAFBAT_UI_PASSWORD =
            "Aa1!" + (New-RandomSecret)
        SEQ_ADMIN_PASSWORD =
            "Aa1!" + (New-RandomSecret)
        JWT_SIGNING_KEY = New-RandomSecret 48
        IDENTITY_ADMIN_EMAIL =
            "admin.final.audit@filemanagement.local"
        IDENTITY_ADMIN_PASSWORD =
            "Aa1!" + (New-RandomSecret)
    }

    foreach ($entry in $ports.GetEnumerator()) {
        $values[$entry.Key] = [string]$entry.Value
    }

    foreach ($entry in $values.GetEnumerator()) {
        [Environment]::SetEnvironmentVariable(
            $entry.Key,
            [string]$entry.Value,
            "Process")
    }
}

function Restore-Environment {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable(
            $name,
            $environmentBackup[$name],
            "Process")
    }
}

function Assert-PortAvailable {
    param(
        [int] $Port
    )

    $listener =
        [System.Net.Sockets.TcpListener]::new(
            [System.Net.IPAddress]::Loopback,
            $Port)

    try {
        $listener.Start()
    }
    catch {
        throw "Required isolated port $Port is already in use."
    }
    finally {
        $listener.Stop()
    }
}

function Invoke-Docker {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [switch] $CaptureOutput
    )

    if ($CaptureOutput) {
        $output = @(& docker @Arguments 2>&1)

        if ($LASTEXITCODE -ne 0) {
            throw (
                "Docker command failed with exit code " +
                "$LASTEXITCODE.`n" +
                ($output -join [Environment]::NewLine)
            )
        }

        return $output
    }

    & docker @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Docker command failed with exit code $LASTEXITCODE."
    }
}

function Invoke-Compose {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [switch] $CaptureOutput
    )

    Invoke-Docker `
        -Arguments ($composeArguments + $Arguments) `
        -CaptureOutput:$CaptureOutput
}

function Send-HttpRequest {
    param(
        [Parameter(Mandatory)]
        [string] $Uri,

        [string] $Method = "GET",

        [hashtable] $Headers = @{},

        [System.Net.Http.HttpContent] $Content
    )

    $request =
        [System.Net.Http.HttpRequestMessage]::new(
            [System.Net.Http.HttpMethod]::new($Method),
            $Uri)

    try {
        foreach ($entry in $Headers.GetEnumerator()) {
            if (
                !$request.Headers.TryAddWithoutValidation(
                    $entry.Key,
                    [string]$entry.Value)
            )
            {
                throw "HTTP header could not be added: $($entry.Key)"
            }
        }

        if ($null -ne $Content) {
            $request.Content = $Content
        }

        $responseTask =
            $client.SendAsync($request)

        $response =
            $responseTask.GetAwaiter().GetResult()

        try {
            $readTask =
                $response.Content.ReadAsByteArrayAsync()

            $bytes =
                $readTask.GetAwaiter().GetResult()

            return [pscustomobject]@{
                StatusCode = [int]$response.StatusCode
                Bytes = $bytes
                Body = [Text.Encoding]::UTF8.GetString($bytes)
            }
        }
        finally {
            $response.Dispose()
        }
    }
    finally {
        $request.Dispose()
    }
}

function Assert-StatusCode {
    param(
        [Parameter(Mandatory)]
        $Response,

        [Parameter(Mandatory)]
        [int] $Expected,

        [Parameter(Mandatory)]
        [string] $Description
    )

    if ($Response.StatusCode -ne $Expected) {
        throw (
            "$Description returned HTTP $($Response.StatusCode); " +
            "expected $Expected. $($Response.Body)"
        )
    }
}

function Wait-Until {
    param(
        [Parameter(Mandatory)]
        [scriptblock] $Condition,

        [Parameter(Mandatory)]
        [string] $Description,

        [int] $TimeoutSeconds = 60,

        [int] $IntervalSeconds = 2
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)

    do {
        if (& $Condition) {
            return
        }

        Start-Sleep -Seconds $IntervalSeconds
    }
    while ([DateTime]::UtcNow -lt $deadline)

    throw "Timed out waiting for $Description."
}

function Invoke-DatabaseScalar {
    param(
        [Parameter(Mandatory)]
        [string] $Sql
    )

    $output =
        Invoke-Compose `
            -Arguments @(
                "exec"
                "--no-TTY"
                "postgres"
                "psql"
                "--username"
                $env:POSTGRES_USER
                "--dbname"
                $env:POSTGRES_DB
                "--tuples-only"
                "--no-align"
                "--command"
                $Sql
            ) `
            -CaptureOutput

    return (($output -join "`n").Trim())
}

function Get-Sha256 {
    param(
        [Parameter(Mandatory)]
        [byte[]] $Bytes
    )

    $algorithm =
        [System.Security.Cryptography.SHA256]::Create()

    try {
        $hash =
            [BitConverter]::ToString(
                $algorithm.ComputeHash($Bytes))

        return $hash.Replace(
            "-",
            "")
    }
    finally {
        $algorithm.Dispose()
    }
}

try {
    Set-Location $repoRoot
    [Environment]::CurrentDirectory = $repoRoot

    Write-Host "`n=== Isolated E2E preflight ==="

    if (!(Test-Path -LiteralPath $composeFile)) {
        throw "Compose file was not found."
    }

    if (!(Test-Path -LiteralPath $envFile)) {
        throw "Example environment file was not found."
    }

    $dockerVersion =
        Invoke-Docker `
            -Arguments @(
                "version"
                "--format"
                "{{.Server.Version}}"
            ) `
            -CaptureOutput

    Write-Host "Docker server: $($dockerVersion -join '')"

    Set-IsolatedEnvironment

    foreach ($port in $ports.Values) {
        Assert-PortAvailable -Port $port
    }

    $existingContainers =
        @(
            @(
            Invoke-Docker `
                -Arguments @(
                    "ps"
                    "--all"
                    "--quiet"
                    "--filter"
                    "label=com.docker.compose.project=$projectName"
                ) `
                -CaptureOutput
            ) |
            Where-Object {
                ![string]::IsNullOrWhiteSpace([string]$_)
            }
        )

    if ($existingContainers.Count -ne 0) {
        throw (
            "The isolated Compose project already has containers. " +
            "No existing resources were changed: $projectName"
        )
    }

    $services =
        @(
            @(
            Invoke-Compose `
                -Arguments @(
                    "config"
                    "--services"
                ) `
                -CaptureOutput
            ) |
            Where-Object {
                ![string]::IsNullOrWhiteSpace([string]$_)
            }
        )

    if ($services.Count -ne $expectedServiceCount) {
        throw (
            "Compose has $($services.Count) services; " +
            "$expectedServiceCount were expected."
        )
    }

    Invoke-Compose `
        -Arguments @(
            "config"
            "--quiet"
        )

    Write-Host "Compose services: $($services.Count)"
    Write-Host "Dedicated ports: available"
    Write-Host "Existing project resources: none"

    Write-Host "`n=== Build and clean startup ==="

    $environmentStarted = $true

    Invoke-Compose `
        -Arguments @(
            "up"
            "--detach"
            "--build"
            "--wait"
            "--wait-timeout"
            "420"
        )

    $runningServices =
        @(
            @(
            Invoke-Compose `
                -Arguments @(
                    "ps"
                    "--services"
                    "--filter"
                    "status=running"
                ) `
                -CaptureOutput
            ) |
            Where-Object {
                ![string]::IsNullOrWhiteSpace([string]$_)
            }
        )

    $exitedServices =
        @(
            @(
            Invoke-Compose `
                -Arguments @(
                    "ps"
                    "--services"
                    "--filter"
                    "status=exited"
                ) `
                -CaptureOutput
            ) |
            Where-Object {
                ![string]::IsNullOrWhiteSpace([string]$_)
            }
        )

    if ($runningServices.Count -ne $expectedRunningServiceCount) {
        throw (
            "Running service count is $($runningServices.Count); " +
            "expected $expectedRunningServiceCount."
        )
    }

    if ($exitedServices.Count -ne $expectedOneShotServiceCount) {
        throw (
            "Completed one-shot service count is " +
            "$($exitedServices.Count); expected " +
            "$expectedOneShotServiceCount."
        )
    }

    Write-Host "Running long-lived services: $($runningServices.Count)"
    Write-Host "Completed one-shot services: $($exitedServices.Count)"

    Write-Host "`n=== Health and authorization boundaries ==="

    $healthUris = @(
        "http://127.0.0.1:$($ports.WEB_PORT)/health"
        "http://127.0.0.1:$($ports.GATEWAY_PORT)/health"
        "http://127.0.0.1:$($ports.API_PORT)/health"
        "http://127.0.0.1:$($ports.IDENTITY_API_PORT)/health"
        "http://127.0.0.1:$($ports.REPORTING_PORT)/health"
        "http://127.0.0.1:$($ports.KAFBAT_UI_PORT)/actuator/health"
        "http://127.0.0.1:$($ports.REDISINSIGHT_PORT)/api/health/"
    )

    foreach ($uri in $healthUris) {
        $response = Send-HttpRequest -Uri $uri

        Assert-StatusCode `
            -Response $response `
            -Expected 200 `
            -Description $uri
    }

    $reportingSwagger =
        Send-HttpRequest `
            -Uri (
                "http://127.0.0.1:" +
                "$($ports.REPORTING_PORT)/swagger/index.html"
            )

    Assert-StatusCode `
        -Response $reportingSwagger `
        -Expected 200 `
        -Description "Reporting Swagger UI"

    $reportingOpenApi =
        Send-HttpRequest `
            -Uri (
                "http://127.0.0.1:" +
                "$($ports.REPORTING_PORT)/openapi/v1.json"
            )

    Assert-StatusCode `
        -Response $reportingOpenApi `
        -Expected 200 `
        -Description "Reporting OpenAPI document"

    $reportingOpenApiDocument =
        $reportingOpenApi.Body |
        ConvertFrom-Json

    if (
        $reportingOpenApiDocument.components.securitySchemes.Basic.scheme -ne
        "basic"
    )
    {
        throw "Reporting OpenAPI Basic security scheme was not found."
    }

    $gatewayBase =
        "http://127.0.0.1:$($ports.GATEWAY_PORT)"

    $anonymousFiles =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/files"

    Assert-StatusCode `
        -Response $anonymousFiles `
        -Expected 401 `
        -Description "Anonymous file list"

    $invalidLoginContent =
        [System.Net.Http.StringContent]::new(
            (@{
                email = $env:IDENTITY_ADMIN_EMAIL
                password = "deliberately-wrong"
            } | ConvertTo-Json),
            [Text.Encoding]::UTF8,
            "application/json")

    $invalidLogin =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/auth/login" `
            -Method "POST" `
            -Content $invalidLoginContent

    Assert-StatusCode `
        -Response $invalidLogin `
        -Expected 401 `
        -Description "Invalid login"

    $loginContent =
        [System.Net.Http.StringContent]::new(
            (@{
                email = $env:IDENTITY_ADMIN_EMAIL
                password = $env:IDENTITY_ADMIN_PASSWORD
            } | ConvertTo-Json),
            [Text.Encoding]::UTF8,
            "application/json")

    $login =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/auth/login" `
            -Method "POST" `
            -Content $loginContent

    Assert-StatusCode `
        -Response $login `
        -Expected 200 `
        -Description "Administrator login"

    $loginBody = $login.Body | ConvertFrom-Json

    if (
        [string]::IsNullOrWhiteSpace(
            [string]$loginBody.accessToken)
    )
    {
        throw "Login did not return an access token."
    }

    $bearerHeaders = @{
        Authorization = "Bearer $($loginBody.accessToken)"
    }

    $currentUser =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/auth/me" `
            -Headers $bearerHeaders

    Assert-StatusCode `
        -Response $currentUser `
        -Expected 200 `
        -Description "Current user"

    $adminPing =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/auth/admin/ping" `
            -Headers $bearerHeaders

    Assert-StatusCode `
        -Response $adminPing `
        -Expected 200 `
        -Description "Administrator authorization"

    Write-Host "Health endpoints: 7/7"
    Write-Host "Anonymous file access: 401"
    Write-Host "Invalid login: 401"
    Write-Host "Administrator JWT flow: valid"

    Write-Host "`n=== File lifecycle and Redis fallback ==="

    $pngBytes =
        [Convert]::FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=")

    $multipart =
        [System.Net.Http.MultipartFormDataContent]::new()

    $fileContent =
        [System.Net.Http.ByteArrayContent]::new($pngBytes)

    $fileContent.Headers.ContentType =
        [System.Net.Http.Headers.MediaTypeHeaderValue]::new(
            "image/png")

    $multipart.Add(
        $fileContent,
        "file",
        "final-audit.png")

    $multipart.Add(
        [System.Net.Http.StringContent]::new("FinalAudit"),
        "relatedRecordType")

    $multipart.Add(
        [System.Net.Http.StringContent]::new("isolated-e2e"),
        "relatedRecordId")

    $upload =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/files" `
            -Method "POST" `
            -Headers $bearerHeaders `
            -Content $multipart

    Assert-StatusCode `
        -Response $upload `
        -Expected 201 `
        -Description "File upload"

    $uploadedFile = $upload.Body | ConvertFrom-Json
    $fileId = [Guid]::Parse([string]$uploadedFile.id)

    $listUri =
        "$gatewayBase/api/files?" +
        "relatedRecordType=FinalAudit&" +
        "relatedRecordId=isolated-e2e"

    $list =
        Send-HttpRequest `
            -Uri $listUri `
            -Headers $bearerHeaders

    Assert-StatusCode `
        -Response $list `
        -Expected 200 `
        -Description "File list"

    $listedFiles = @($list.Body | ConvertFrom-Json)

    if (
        @(
            $listedFiles |
            Where-Object {
                [string]$_.id -eq $fileId.ToString()
            }
        ).Count -ne 1
    )
    {
        throw "Uploaded file was not returned by the list endpoint."
    }

    $detail =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/files/$fileId" `
            -Headers $bearerHeaders

    Assert-StatusCode `
        -Response $detail `
        -Expected 200 `
        -Description "File detail"

    Invoke-Compose `
        -Arguments @(
            "stop"
            "redis"
        )

    $listWithoutRedis =
        Send-HttpRequest `
            -Uri $listUri `
            -Headers $bearerHeaders

    Assert-StatusCode `
        -Response $listWithoutRedis `
        -Expected 200 `
        -Description "File list while Redis is unavailable"

    Invoke-Compose `
        -Arguments @(
            "start"
            "--wait"
            "redis"
        )

    $download =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/files/$fileId/download" `
            -Headers $bearerHeaders

    Assert-StatusCode `
        -Response $download `
        -Expected 200 `
        -Description "File download"

    if (
        (Get-Sha256 -Bytes $download.Bytes) -ne
        (Get-Sha256 -Bytes $pngBytes)
    )
    {
        throw "Downloaded file hash does not match the upload."
    }

    $preview =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/files/$fileId/preview" `
            -Headers $bearerHeaders

    Assert-StatusCode `
        -Response $preview `
        -Expected 200 `
        -Description "File preview"

    if (
        (Get-Sha256 -Bytes $preview.Bytes) -ne
        (Get-Sha256 -Bytes $pngBytes)
    )
    {
        throw "Preview file hash does not match the upload."
    }

    $presigned =
        Send-HttpRequest `
            -Uri (
                "$gatewayBase/api/files/$fileId/" +
                "presigned-url?expiresInMinutes=5"
            ) `
            -Headers $bearerHeaders

    Assert-StatusCode `
        -Response $presigned `
        -Expected 200 `
        -Description "Presigned URL generation"

    $presignedBody = $presigned.Body | ConvertFrom-Json

    $presignedDownload =
        Send-HttpRequest `
            -Uri ([string]$presignedBody.url)

    Assert-StatusCode `
        -Response $presignedDownload `
        -Expected 200 `
        -Description "Presigned file download"

    if (
        (Get-Sha256 -Bytes $presignedDownload.Bytes) -ne
        (Get-Sha256 -Bytes $pngBytes)
    )
    {
        throw "Presigned file hash does not match the upload."
    }

    $delete =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/files/$fileId" `
            -Method "DELETE" `
            -Headers $bearerHeaders

    Assert-StatusCode `
        -Response $delete `
        -Expected 204 `
        -Description "File delete"

    $missingDetail =
        Send-HttpRequest `
            -Uri "$gatewayBase/api/files/$fileId" `
            -Headers $bearerHeaders

    Assert-StatusCode `
        -Response $missingDetail `
        -Expected 404 `
        -Description "Deleted file detail"

    Write-Host "Upload/list/detail: valid"
    Write-Host "Redis unavailable fallback: valid"
    Write-Host "Download/preview/presigned hashes: equal"
    Write-Host "Delete and not-found boundary: valid"

    Write-Host "`n=== Outbox, Kafka and reporting ==="

    Wait-Until `
        -Description "pending outbox count to reach zero" `
        -TimeoutSeconds 90 `
        -Condition {
            (Invoke-DatabaseScalar `
                -Sql (
                    "SELECT count(*) FROM outbox_messages " +
                    "WHERE processed_at_utc IS NULL;"
                )) -eq "0"
        }

    $operationCounts =
        Invoke-DatabaseScalar `
            -Sql (
                "SELECT payload::jsonb #>> " +
                "'{payload,operation}', count(*) " +
                "FROM outbox_messages " +
                "GROUP BY 1 ORDER BY 1;"
            )

    foreach (
        $expected in @(
            "deleted|1"
            "downloaded|1"
            "uploaded|1"
        )
    )
    {
        if (
            @($operationCounts -split "`r?`n") -notcontains
                $expected
        )
        {
            throw (
                "Expected outbox operation count was not found: " +
                $expected
            )
        }
    }

    $consumerGroupOutput =
        Invoke-Compose `
            -Arguments @(
                "exec"
                "--no-TTY"
                "kafka"
                "/opt/kafka/bin/kafka-consumer-groups.sh"
                "--bootstrap-server"
                "kafka:19092"
                "--describe"
                "--group"
                "operations-worker-v1"
            ) `
            -CaptureOutput

    $lagValues = @()

    foreach ($line in $consumerGroupOutput) {
        if (
            [string]$line -match
            "^\s*operations-worker-v1\s+\S+\s+\d+\s+\d+\s+\d+\s+(\d+)"
        )
        {
            $lagValues += [int]$Matches[1]
        }
    }

    if ($lagValues.Count -eq 0) {
        throw "Kafka consumer lag could not be parsed."
    }

    $maximumLag =
        ($lagValues | Measure-Object -Maximum).Maximum

    if ($maximumLag -ne 0) {
        throw "Kafka maximum consumer lag is $maximumLag; expected 0."
    }

    $reportingBase =
        "http://127.0.0.1:$($ports.REPORTING_PORT)"

    $anonymousDashboard =
        Send-HttpRequest `
            -Uri "$reportingBase/hangfire"

    Assert-StatusCode `
        -Response $anonymousDashboard `
        -Expected 401 `
        -Description "Anonymous Hangfire dashboard"

    $basicValue =
        [Convert]::ToBase64String(
            [Text.Encoding]::ASCII.GetBytes(
                $env:REPORTING_DASHBOARD_USERNAME +
                ":" +
                $env:REPORTING_DASHBOARD_PASSWORD))

    $basicHeaders = @{
        Authorization = "Basic $basicValue"
    }

    $authorizedDashboard =
        Send-HttpRequest `
            -Uri "$reportingBase/hangfire" `
            -Headers $basicHeaders

    Assert-StatusCode `
        -Response $authorizedDashboard `
        -Expected 200 `
        -Description "Authorized Hangfire dashboard"

    $reportDate = [DateTime]::UtcNow.ToString("yyyy-MM-dd")

    $enqueue =
        Send-HttpRequest `
            -Uri (
                "$reportingBase/api/reports/" +
                "daily/$reportDate/enqueue"
            ) `
            -Method "POST" `
            -Headers $basicHeaders

    Assert-StatusCode `
        -Response $enqueue `
        -Expected 202 `
        -Description "Manual daily report enqueue"

    Wait-Until `
        -Description "daily report generation" `
        -TimeoutSeconds 90 `
        -Condition {
            return (
                Invoke-DatabaseScalar `
                    -Sql (
                        "SELECT count(*) " +
                        "FROM daily_file_operation_reports " +
                        "WHERE report_date = " +
                        "'$reportDate';"
                    )
            ) -eq "1"
        }

    $reportsResponse =
        Send-HttpRequest `
            -Uri (
                "$reportingBase/api/reports/" +
                "daily?limit=10"
            ) `
            -Headers $basicHeaders

    Assert-StatusCode `
        -Response $reportsResponse `
        -Expected 200 `
        -Description "Daily reports"

    if (
        $reportsResponse.Body -notmatch
            [Regex]::Escape($reportDate)
    ) {
        throw (
            "The reporting API did not return the generated " +
            "report date."
        )
    }

    $dailyReportCounts =
        Invoke-DatabaseScalar `
            -Sql (
                "SELECT uploaded_count || '|' || " +
                "downloaded_count || '|' || " +
                "deleted_count || '|' || " +
                "pending_outbox_count || '|' || " +
                "failed_outbox_count || '|' || " +
                "invalid_event_count " +
                "FROM daily_file_operation_reports " +
                "WHERE report_date = '$reportDate';"
            )

    if ($dailyReportCounts -ne "1|1|1|0|0|0") {
        throw (
            "Daily report counts were unexpected: " +
            $dailyReportCounts
        )
    }

    Write-Host "Outbox operations: uploaded=1, downloaded=1, deleted=1"
    Write-Host "Pending outbox: 0"
    Write-Host "Maximum Kafka lag: 0"
    Write-Host "Hangfire dashboard: anonymous=401, authorized=200"
    Write-Host "Daily report: generated and verified"

    Write-Host "`n=== Isolated E2E result ==="
    Write-Host "All runtime checks passed."
}
catch {
    Write-Host "`n=== Isolated E2E failed ==="
    Write-Host $_.Exception.Message

    if ($environmentStarted) {
        try {
            Invoke-Compose `
                -Arguments @(
                    "ps"
                    "--all"
                )

            Invoke-Compose `
                -Arguments @(
                    "logs"
                    "--tail"
                    "80"
                    "api"
                    "identity-api"
                    "gateway"
                    "outbox-worker"
                    "operations-worker"
                    "reporting-worker"
                )
        }
        catch {
            Write-Warning (
                "Diagnostic output could not be collected: " +
                $_.Exception.Message
            )
        }
    }

    throw
}
finally {
    $client.Dispose()

    if ($environmentStarted) {
        Write-Host "`n=== Isolated environment cleanup ==="

        try {
            if (
                $projectName -ne
                "minio-file-management-final-audit"
            )
            {
                throw "Unexpected Compose project name; cleanup aborted."
            }

            Invoke-Compose `
                -Arguments @(
                    "down"
                    "--volumes"
                    "--remove-orphans"
                    "--timeout"
                    "20"
                )

            Write-Host (
                "Removed only the isolated project's " +
                "containers, network and volumes."
            )
        }
        catch {
            Write-Warning (
                "Isolated environment cleanup failed: " +
                $_.Exception.Message
            )
        }
    }

    Restore-Environment
}
