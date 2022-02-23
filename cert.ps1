$cert = New-SelfSignedCertificate -DnsName "chonkmarket.dev", "chonkmarket.dev" -CertStoreLocation "cert:\LocalMachine\My" -NotAfter (Get-Date).AddYears(5)
$thumb = $cert.GetCertHashString()

For ($i=44300; $i -le 44399; $i++) {
    netsh http delete sslcert ipport=0.0.0.0:$i
}

For ($i=44300; $i -le 44399; $i++) {
    netsh http add sslcert ipport=0.0.0.0:$i certhash=$thumb appid=`{214124cd-d05b-4309-9af9-9caa44b2b74a`}
}

$StoreScope = 'LocalMachine'
$StoreName = 'root'

$Store = New-Object  -TypeName System.Security.Cryptography.X509Certificates.X509Store  -ArgumentList $StoreName, $StoreScope
$Store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
$Store.Add($cert)

$Store.Close()