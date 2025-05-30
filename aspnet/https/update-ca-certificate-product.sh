#!/bin/sh

echo "[Product Service] Startup script is running"

# Trust identity-service certificate
cp /app/https/identity.crt /usr/local/share/ca-certificates/identity.crt
update-ca-certificates --verbose

echo "[Product Service] Starting app..."
exec dotnet MSA.ProductService.dll
