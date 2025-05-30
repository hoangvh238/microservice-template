#!/bin/sh

echo "[Identity] Startup script is running"

# Identity service does not need to trust its own cert
exec dotnet MSA.IdentityServer.dll
