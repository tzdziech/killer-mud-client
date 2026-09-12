param(
    [ValidateSet("User", "Admin")]
    [string]$Configuration = "User"
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$solution = Join-Path $repoRoot "KillerMudClient.sln"
$appProject = Join-Path $repoRoot "src\MudClient.App\MudClient.App.csproj"

dotnet restore $solution
dotnet build $solution --configuration $Configuration
dotnet run --project $appProject --configuration $Configuration --no-build
