function New-Guid {
    [System.Guid]::NewGuid().ToString("d").Substring(0, 6).Replace("-", "")
}
