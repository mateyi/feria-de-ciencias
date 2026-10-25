# Extrae los streams FlateDecode del PDF del plan de trabajo y los vuelca a texto.
$ErrorActionPreference = 'Stop'
$path = 'D:\feria-de-ciencias\archivos\Mundial Runner - Plan de Trabajo-MateoGimenez-MateoGonzalez.pdf'
$bytes = [System.IO.File]::ReadAllBytes($path)
$latin = [System.Text.Encoding]::GetEncoding(28591).GetString($bytes)

$sb = New-Object System.Text.StringBuilder
$idx = 0
while (($idx = $latin.IndexOf('stream', $idx)) -ge 0) {
    $start = $idx + 6
    while ($start -lt $latin.Length -and ($latin[$start] -eq "`r" -or $latin[$start] -eq "`n")) { $start++ }
    $end = $latin.IndexOf('endstream', $start)
    if ($end -lt 0) { break }
    $len = $end - $start
    if ($len -gt 4) {
        try {
            $ms = New-Object System.IO.MemoryStream ($bytes, $start, $len)
            $ms.ReadByte() | Out-Null
            $ms.ReadByte() | Out-Null
            $ds = New-Object System.IO.Compression.DeflateStream($ms, [System.IO.Compression.CompressionMode]::Decompress)
            $out = New-Object System.IO.MemoryStream
            $ds.CopyTo($out)
            [void]$sb.Append([System.Text.Encoding]::GetEncoding(28591).GetString($out.ToArray()))
            [void]$sb.Append("`n")
        } catch { }
    }
    $idx = $end + 9
}

$content = $sb.ToString()

# Quedarse solo con los literales de texto de los operadores Tj/TJ.
$textos = New-Object System.Text.StringBuilder
foreach ($m in [regex]::Matches($content, '\((?:\\.|[^\\()])*\)\s*T[jJ]')) {
    $t = $m.Value
    $t = $t.Substring(1, $t.IndexOf(')') - 1)
    $t = $t -replace '\\\(', '(' -replace '\\\)', ')' -replace '\\\\', '\'
    [void]$textos.Append($t)
    [void]$textos.Append("`n")
}

Set-Content -Path 'D:\feria-de-ciencias\archivos\_plan_texto.txt' -Value $textos.ToString() -Encoding UTF8
Set-Content -Path 'D:\feria-de-ciencias\archivos\_plan_crudo.txt' -Value $content -Encoding UTF8
Write-Output ('decompressed=' + $content.Length + ' text=' + $textos.Length)
