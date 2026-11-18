try {
  $o=New-Object -ComObject 'BPwinModel'
  Write-Output ("OK " + $o.GetType().FullName)
  $o | Get-Member | Select-Object -First 20
} catch {
  Write-Output ("ERR " + $_.Exception.Message)
}
