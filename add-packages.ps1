# Add Microsoft.CodeAnalysis packages to Codler.csproj
$projectPath = "C:\Users\avnee\source\repos\Codler\Codler\Codler.csproj"

$content = Get-Content $projectPath

# Find the line with Microsoft.VSSDK.BuildTools and add packages after it
$newContent = $content -replace `
    '(<PackageReference Include="Microsoft.VSSDK.BuildTools"[^>]*/>)', `
    ('$1' + [Environment]::NewLine + '    <PackageReference Include="Microsoft.CodeAnalysis" Version="3.11.0" />' + [Environment]::NewLine + '    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="3.11.0" />')

Set-Content $projectPath $newContent

Write-Host "? Added Microsoft.CodeAnalysis packages to Codler.csproj"
