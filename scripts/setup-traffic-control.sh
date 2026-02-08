#!/bin/bash

APISIX_ADMIN_URL="http://127.0.0.1:9080/apisix/admin"
API_KEY="edd1c9f034335f136f87ad84b625c8f1"

echo "Configuring Traffic Mirroring & Canary Release..."

# 1. 流量鏡像 (Traffic Mirroring)
# 將生產環境 (Upstream 1) 的流量複製一份到測試環境 (Upstream 2)
curl -i -X PUT $APISIX_ADMIN_URL/routes/mirror-route \
-H "X-API-KEY: $API_KEY" \
-d '{
    "uri": "/api/mirror/*",
    "plugins": {
        "proxy-mirror": {
            "host": "http://test-backend:8080"
        }
    },
    "upstream": {
        "nodes": {
            "prod-backend:8080": 1
        },
        "type": "roundrobin"
    }
}'

# 2. 灰度發佈 (Canary Release) - 基於 Header
# 帶有 "X-Canary: true" 的請求轉發到新版本
curl -i -X PUT $APISIX_ADMIN_URL/routes/canary-route \
-H "X-API-KEY: $API_KEY" \
-d '{
    "uri": "/api/canary/*",
    "plugins": {
        "traffic-split": {
            "rules": [
                {
                    "match": [
                        {
                            "vars": [
                                ["http_x_canary", "==", "true"]
                            ]
                        }
                    ],
                    "weighted_upstreams": [
                        {
                            "upstream": {
                                "name": "canary-upstream",
                                "type": "roundrobin",
                                "nodes": {
                                    "new-version-backend:8080": 1
                                }
                            }
                        }
                    ]
                }
            ]
        }
    },
    "upstream": {
        "nodes": {
            "stable-backend:8080": 1
        },
        "type": "roundrobin"
    }
}'

echo "Done."
