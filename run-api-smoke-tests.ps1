param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$UserToken = "",
    [string]$OwnerToken = ""
)
$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd("/")
$failed = 0
$passed = 0
function Check([string]$name,[string]$path,[int]$expected,[string]$token="") {
    $headers = @{}
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    try {
        # Windows PowerShell 5.1 throws for 4xx/5xx; read the HTTP status from the exception.
        try {
            $response = Invoke-WebRequest -Uri "$base$path" -Headers $headers -Method Get -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 15
            $actual = [int]$response.StatusCode
        } catch [System.Net.WebException] {
            if ($null -eq $_.Exception.Response) { throw }
            $actual = [int]$_.Exception.Response.StatusCode
        }
        if ($actual -eq $expected) {
            $script:passed++
            Write-Host "PASS $name ($actual)"
        } else {
            $script:failed++
            Write-Host "FAIL $name expected=$expected actual=$actual"
        }
    } catch {
        $script:failed++
        Write-Host "FAIL $name $($_.Exception.Message)"
    }
}
Write-Host "Checking LOCAL API at $base (read-only requests only)"
Check "Swagger UI" "/swagger/index.html" 200
Check "Swagger JSON" "/swagger/v1/swagger.json" 200
Check "Public categories" "/api/categories" 200
Check "Missing item" "/api/items/2147483647" 404
Check "Invalid pagination" "/api/items?page=0&pageSize=20" 400
Check "Unauthenticated loans" "/api/loans" 401
Check "Unauthenticated site admin" "/api/site-admin/users" 401
if ($UserToken) {
    Check "Ordinary user forbidden owner loans" "/api/loans" 403 $UserToken
    Check "Ordinary user forbidden site admin" "/api/site-admin/users" 403 $UserToken
}
if ($OwnerToken) {
    Check "Owner loans" "/api/loans" 200 $OwnerToken
    Check "Owner forbidden site admin" "/api/site-admin/users" 403 $OwnerToken
}
Write-Host "PASS=$passed FAIL=$failed"
if ($failed -gt 0) { exit 1 }
exit 0
