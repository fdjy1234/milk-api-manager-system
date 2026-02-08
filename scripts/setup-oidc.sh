#!/bin/bash

APISIX_ADMIN_URL="http://127.0.0.1:9080/apisix/admin"
API_KEY="edd1c9f034335f136f87ad84b625c8f1"

# 假設的 Keycloak 設定 (實際部署時需替換)
KEYCLOAK_DISCOVERY="http://keycloak:8080/realms/milk-realm/.well-known/openid-configuration"
CLIENT_ID="apisix-client"
CLIENT_SECRET="your-client-secret"

echo "Configuring Keycloak OIDC Integration..."

# 建立一個受 OIDC 保護的路由
curl -i -X PUT $APISIX_ADMIN_URL/routes/oidc-protected-route \
-H "X-API-KEY: $API_KEY" \
-d '{
    "uri": "/api/protected/*",
    "plugins": {
        "openid-connect": {
            "client_id": "'"$CLIENT_ID"'",
            "client_secret": "'"$CLIENT_SECRET"'",
            "discovery": "'"$KEYCLOAK_DISCOVERY"'",
            "scope": "openid profile email",
            "bearer_only": true,
            "realm": "milk-realm",
            "introspection_endpoint_auth_method": "client_secret_basic"
        }
    },
    "upstream": {
        "nodes": {
            "backend-service:8080": 1
        },
        "type": "roundrobin"
    }
}'

echo "Done. Route /api/protected/* is now secured by Keycloak."
