#!/bin/sh
set -e

# Detect DNS resolver from /etc/resolv.conf (CoreDNS in Azure Container Apps / Kubernetes, 127.0.0.11 in Docker)
NAMESERVERS=$(awk 'BEGIN{ORS=" "} $1=="nameserver" {print $2}' /etc/resolv.conf)
NAMESERVERS="${NAMESERVERS% }"

if [ -n "$NAMESERVERS" ]; then
    echo "[40-dynamic-resolver.sh] Detected DNS resolver(s): ${NAMESERVERS}"
    sed -i "s|resolver .*|resolver ${NAMESERVERS} valid=10s ipv6=off;|" /etc/nginx/conf.d/default.conf
fi
