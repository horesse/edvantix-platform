#!/bin/sh
set -e

# Fail fast on missing required values rather than serve a broken bundle.
: "${EDV_API_URL:?EDV_API_URL is required (e.g. https://api.example.com)}"
: "${EDV_DASHBOARD_URL:?EDV_DASHBOARD_URL is required (e.g. https://app.example.com)}"

# Defaults for non-required values.
: "${EDV_DEFAULT_TENANT:=root}"

export EDV_API_URL EDV_DASHBOARD_URL EDV_DEFAULT_TENANT

# Render the runtime config from the template, writing into nginx's web root.
envsubst < /usr/share/nginx/html/config.json.template > /usr/share/nginx/html/config.json

# Drop the template so it isn't served accidentally.
rm /usr/share/nginx/html/config.json.template

exec nginx -g 'daemon off;'
