#!/bin/sh

echo "[Reverse Proxy] Startup script is running"

# Trust identity-service certificate
cp /app/https/identity.crt /usr/local/share/ca-certificates/identity.crt
update-ca-certificates --verbose

echo "[Reverse Proxy] Starting app..."
exec dotnet MSA.ReverseProxy.dll
