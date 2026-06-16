$root = Join-Path $PSScriptRoot "..\Assets\Audio\SFX"
$music = Join-Path $PSScriptRoot "..\Assets\Audio\Music"
New-Item -ItemType Directory -Force -Path $root | Out-Null
New-Item -ItemType Directory -Force -Path $music | Out-Null

$sr = 44100

function Write-Wav {
    param(
        [string]$Path,
        [double[]]$Samples
    )

    $stream = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($stream)
    $writer.Write([char[]]@('R','I','F','F'))
    $writer.Write([int32]0)
    $writer.Write([char[]]@('W','A','V','E'))
    $writer.Write([char[]]@('f','m','t',' '))
    $writer.Write([int32]16)
    $writer.Write([int16]1)
    $writer.Write([int16]1)
    $writer.Write([int32]$sr)
    $writer.Write([int32]($sr * 2))
    $writer.Write([int16]2)
    $writer.Write([int16]16)
    $writer.Write([char[]]@('d','a','t','a'))

    $data = New-Object System.Collections.Generic.List[byte]
    foreach ($sample in $Samples) {
        $clamped = [Math]::Max(-1.0, [Math]::Min(1.0, $sample))
        $value = [int16]($clamped * 32767)
        $bytes = [BitConverter]::GetBytes($value)
        [void]$data.AddRange($bytes)
    }

    $writer.Write([int32]$data.Count)
    $writer.Write($data.ToArray())
    $bytesAll = $stream.ToArray()
    $size = $bytesAll.Length - 8
    [Array]::Copy([BitConverter]::GetBytes([int32]$size), 0, $bytesAll, 4, 4)
    [System.IO.File]::WriteAllBytes($Path, $bytesAll)
    $writer.Close()
    $stream.Close()
}

function Get-Envelope {
    param(
        [double]$T,
        [double]$Duration,
        [double]$Attack = 0.01,
        [double]$Release = 0.05
    )

    if ($T -lt 0 -or $T -gt $Duration) { return 0.0 }
    if ($T -lt $Attack) {
        if ($Attack -eq 0) { return 1.0 }
        return $T / $Attack
    }
    if ($T -gt ($Duration - $Release)) {
        if ($Release -eq 0) { return 0.0 }
        return [Math]::Max(0.0, ($Duration - $T) / $Release)
    }
    return 1.0
}

function Get-Sine {
    param([double]$Freq, [double]$T)
    return [Math]::Sin(2 * [Math]::PI * $Freq * $T)
}

function Get-Tone {
    param(
        [double]$Duration,
        [double]$Freq,
        [double]$Volume = 0.5,
        [double]$Attack = 0.005,
        [double]$Release = 0.05
    )

    $count = [int]($sr * $Duration)
    $samples = New-Object double[] $count
    for ($i = 0; $i -lt $count; $i++) {
        $t = $i / $sr
        $samples[$i] = (Get-Sine $Freq $t) * (Get-Envelope $t $Duration $Attack $Release) * $Volume
    }
    return ,$samples
}

function Join-Samples {
    param([double[][]]$Parts)
    $total = ($Parts | ForEach-Object { $_.Length } | Measure-Object -Sum).Sum
    $out = New-Object double[] $total
    $offset = 0
    foreach ($part in $Parts) {
        [Array]::Copy($part, 0, $out, $offset, $part.Length)
        $offset += $part.Length
    }
    return ,$out
}

function Get-CoinPickup {
    $duration = 0.12
    $count = [int]($sr * $duration)
    $samples = New-Object double[] $count
    for ($i = 0; $i -lt $count; $i++) {
        $t = $i / $sr
        $env = Get-Envelope $t $duration 0.001 0.08
        $samples[$i] = ((Get-Sine 2200 $t) * 0.55 + (Get-Sine 3300 $t) * 0.35) * $env * 0.65
    }
    return ,$samples
}

function Get-XpPickup {
    $duration = 0.18
    $count = [int]($sr * $duration)
    $samples = New-Object double[] $count
    for ($i = 0; $i -lt $count; $i++) {
        $t = $i / $sr
        $freq = 600 + ($i / $count) * 500
        $samples[$i] = (Get-Sine $freq $t) * (Get-Envelope $t $duration 0.02 0.07) * 0.45
    }
    return ,$samples
}

function Get-LevelUp {
    return Join-Samples @(
        (Get-Tone 0.09 523 0.42 0.004 0.035),
        (Get-Tone 0.09 659 0.42 0.004 0.035),
        (Get-Tone 0.09 784 0.42 0.004 0.035),
        (Get-Tone 0.09 1047 0.42 0.004 0.035)
    )
}

function Get-ChestOpen {
    return Join-Samples @(
        (Get-Tone 0.11 392 0.38 0.008 0.05),
        (Get-Tone 0.11 494 0.38 0.008 0.05),
        (Get-Tone 0.11 587 0.38 0.008 0.05),
        (Get-Tone 0.11 784 0.38 0.008 0.05)
    )
}

function Get-UpgradeSelect {
    return Get-Tone 0.08 880 0.5 0.002 0.03
}

function Get-BossSpawn {
    $duration = 0.45
    $count = [int]($sr * $duration)
    $samples = New-Object double[] $count
    for ($i = 0; $i -lt $count; $i++) {
        $t = $i / $sr
        $pulse = 0.55 + 0.45 * [Math]::Sin(2 * [Math]::PI * 7 * $t)
        $env = Get-Envelope $t $duration 0.015 0.12
        $samples[$i] = ((Get-Sine 110 $t) * 0.65 + (Get-Sine 73 $t) * 0.35) * $pulse * $env * 0.58
    }
    return ,$samples
}

function Get-GameOver {
    return Join-Samples @(
        (Get-Tone 0.12 440 0.46 0.004 0.06),
        (Get-Tone 0.12 349 0.46 0.004 0.06),
        (Get-Tone 0.12 294 0.46 0.004 0.06),
        (Get-Tone 0.12 220 0.46 0.004 0.06)
    )
}

function Get-ButtonClick {
    return Get-Tone 0.04 1400 0.38 0.001 0.018
}

$files = @{
    "coin_pickup.wav" = Get-CoinPickup
    "xp_pickup.wav" = Get-XpPickup
    "level_up.wav" = Get-LevelUp
    "chest_open.wav" = Get-ChestOpen
    "upgrade_select.wav" = Get-UpgradeSelect
    "boss_spawn.wav" = Get-BossSpawn
    "game_over.wav" = Get-GameOver
    "button_click.wav" = Get-ButtonClick
}

foreach ($entry in $files.GetEnumerator()) {
    $path = Join-Path $root $entry.Key
    Write-Wav -Path $path -Samples $entry.Value
    Write-Output "Created $path"
}
