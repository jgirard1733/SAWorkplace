$web = New-Object System.Net.WebClient
$str = $web.DownloadString("http://localhost:88/api/updateusers")
$str