#!/bin/sh
set -e

# Detect DNS resolver from /etc/resolv.conf (CoreDNS in Azure Container Apps / Kubernetes, 127.0.0.11 in Docker)
NAMESERVERS=$(awk 'BEGIN{ORS=" "} $1=="nameserver" {print $2}' /etc/resolv.conf)
NAMESERVERS="${NAMESERVERS% }"

if [ -n "$NAMESERVERS" ]; then
    echo "[40-dynamic-resolver.sh] Detected DNS resolver(s): ${NAMESERVERS}"
    sed -i "s|resolver .*|resolver ${NAMESERVERS} valid=10s ipv6=off;|" /etc/nginx/conf.d/default.conf
fi

# Detect domain suffix: prioritize explicit ACA_ENVIRONMENT_DOMAIN env var, then search domains from /etc/resolv.conf
DOMAIN_SUFFIX=""
if [ -n "$ACA_ENVIRONMENT_DOMAIN" ]; then
    DOMAIN_SUFFIX="${ACA_ENVIRONMENT_DOMAIN}"
    echo "[40-dynamic-resolver.sh] Using ACA_ENVIRONMENT_DOMAIN: ${DOMAIN_SUFFIX}"
else
    # In Azure Container Apps / Kubernetes, /etc/resolv.conf contains search domains
    K8S_SEARCH=$(awk '$1=="search" {print $2}' /etc/resolv.conf)
    if [ -n "$K8S_SEARCH" ]; then
        DOMAIN_SUFFIX="${K8S_SEARCH}"
        echo "[40-dynamic-resolver.sh] Using resolv.conf search domain: ${DOMAIN_SUFFIX}"
    fi
fi

if [ -n "$DOMAIN_SUFFIX" ]; then
    echo "[40-dynamic-resolver.sh] Reconfiguring upstreams with FQDN suffix: .${DOMAIN_SUFFIX}"
    sed -i "s|student-1-frontend|student-1-frontend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf
    sed -i "s|student-2-frontend|student-2-frontend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf
    sed -i "s|student-3-frontend|student-3-frontend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf
    sed -i "s|student-4-frontend|student-4-frontend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf
    sed -i "s|student-5-frontend|student-5-frontend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf

    sed -i "s|student-1-backend:8080|student-1-backend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf
    sed -i "s|student-2-backend:8080|student-2-backend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf
    sed -i "s|student-3-backend:8080|student-3-backend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf
    sed -i "s|student-4-backend:8080|student-4-backend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf
    sed -i "s|student-4-authentication:8080|student-4-authentication.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf
    sed -i "s|student-5-backend:8080|student-5-backend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf
    sed -i "s|shared-backend:8080|shared-backend.${DOMAIN_SUFFIX}|g" /etc/nginx/conf.d/default.conf

    sed -i "s|Host student-1-backend;|Host student-1-backend.${DOMAIN_SUFFIX};|g" /etc/nginx/conf.d/default.conf
    sed -i "s|Host student-2-backend;|Host student-2-backend.${DOMAIN_SUFFIX};|g" /etc/nginx/conf.d/default.conf
    sed -i "s|Host student-3-backend;|Host student-3-backend.${DOMAIN_SUFFIX};|g" /etc/nginx/conf.d/default.conf
    sed -i "s|Host student-4-backend;|Host student-4-backend.${DOMAIN_SUFFIX};|g" /etc/nginx/conf.d/default.conf
    sed -i "s|Host student-4-authentication;|Host student-4-authentication.${DOMAIN_SUFFIX};|g" /etc/nginx/conf.d/default.conf
    sed -i "s|Host student-5-backend;|Host student-5-backend.${DOMAIN_SUFFIX};|g" /etc/nginx/conf.d/default.conf
    sed -i "s|Host shared-backend;|Host shared-backend.${DOMAIN_SUFFIX};|g" /etc/nginx/conf.d/default.conf
fi
