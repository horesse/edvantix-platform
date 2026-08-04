#!/bin/sh
set -e

: "${EDV_API_URL:?EDV_API_URL is required (e.g. https://api.example.com)}"
: "${EDV_DEFAULT_TENANT:=root}"

export EDV_API_URL EDV_DEFAULT_TENANT

envsubst < /usr/share/nginx/html/config.json.template \
       > /usr/share/nginx/html/config.json
rm /usr/share/nginx/html/config.json.template

exec nginx -g 'daemon off;'
