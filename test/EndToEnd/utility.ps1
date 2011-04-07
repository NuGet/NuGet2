function New-Guid {
    [System.Guid]::NewGuid().ToString("d").Substring(0, 8).Replace("-", "")
}
