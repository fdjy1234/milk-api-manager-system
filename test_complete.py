#!/usr/bin/env python3
"""
Complete Backend API Test Suite
測試所有後端 API 端點
"""

import requests
import json

BASE_URL = "http://localhost:5000"
APISIX_URL = "http://localhost:9180/apisix/admin"
APISIX_KEY = "edd1c9f034335f136f87ad84b625c88b"

def test_api(name, url, method="GET", data=None, headers=None):
    """通用 API 測試函數"""
    print(f"\n{'='*60}")
    print(f"Testing: {name}")
    print(f"URL: {url}")
    print(f"Method: {method}")
    
    try:
        if method == "GET":
            response = requests.get(url, headers=headers, timeout=5)
        elif method == "POST":
            response = requests.post(url, json=data, headers=headers, timeout=5)
        elif method == "DELETE":
            response = requests.delete(url, headers=headers, timeout=5)
        
        print(f"Status: {response.status_code}")
        
        if response.status_code < 400:
            print(f"[PASS] SUCCESS")
            try:
                json_data = response.json()
                print(f"Response: {json.dumps(json_data, indent=2)[:500]}")
            except:
                print(f"Response: {response.text[:200]}")
        else:
            print(f"[FAIL] FAILED - Status {response.status_code}")
            print(f"Response: {response.text[:200]}")
            
        return response
    except Exception as e:
        print(f"[ERROR] ERROR: {str(e)}")
        return None

print("="*60)
print("Milk API Manager - Complete Backend Test")
print("="*60)

# Test 1: APISIX Admin API
print("\n" + "="*60)
print("SECTION 1: APISIX Admin API Tests")
print("="*60)

headers = {"X-API-KEY": APISIX_KEY}
test_api("Get All Routes", f"{APISIX_URL}/routes", headers=headers)
test_api("Get All Consumers", f"{APISIX_URL}/consumers", headers=headers)
test_api("Get All Upstreams", f"{APISIX_URL}/upstreams", headers=headers)

# Test 2: Flask Backend - Blacklist
print("\n" + "="*60)
print("SECTION 2: Flask Blacklist API Tests")
print("="*60)

test_api("Get Blacklist", f"{BASE_URL}/api/Blacklist")

# Add IP to blacklist
test_api(
    "Add IP to Blacklist",
    f"{BASE_URL}/api/Blacklist",
    method="POST",
    data={"ip": "192.168.1.100", "action": "add"}
)

# Test 3: Flask Backend - Consumer Management
print("\n" + "="*60)
print("SECTION 3: Flask Consumer API Tests")
print("="*60)

test_api("Get All Consumers", f"{BASE_URL}/api/Consumer")

# Create a test consumer
test_consumer = {
    "username": "test-user-001",
    "plugins": {
        "key-auth": {
            "key": "test-api-key-12345"
        }
    }
}

test_api(
    "Create Test Consumer",
    f"{BASE_URL}/api/Consumer",
    method="POST",
    data=test_consumer
)

# Test 4: Docker Services
print("\n" + "="*60)
print("SECTION 4: Docker Services Health Check")
print("="*60)

services = [
    ("APISIX Dashboard", "http://localhost:9000"),
    ("Prometheus", "http://localhost:9090/-/ready"),
    ("Grafana", "http://localhost:3000/api/health"),
]

for service_name, url in services:
    try:
        response = requests.get(url, timeout=5)
        status = "✓ UP" if response.status_code < 400 else f"✗ DOWN ({response.status_code})"
        print(f"{service_name:.<40} {status}")
    except Exception as e:
        print(f"{service_name:.<40} ✗ UNREACHABLE")

print("\n" + "="*60)
print("Test Suite Complete")
print("="*60)
