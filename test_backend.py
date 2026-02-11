# Backend Service Test Script
# 測試後端服務的運行狀態

import requests
import json

print("=" * 60)
print("Milk API Manager - Backend Service Test")
print("=" * 60)

# Test 1: APISIX Admin API
print("\n[Test 1] Testing APISIX Admin API...")
try:
    url = "http://localhost:9180/apisix/admin/routes"
    headers = {"X-API-KEY": "edd1c9f034335f136f87ad84b625c88b"}
    response = requests.get(url, headers=headers, timeout=5)
    print(f"   Status: {response.status_code}")
    if response.status_code == 200:
        data = response.json()
        route_count = len(data.get('list', []))
        print(f"   ✓ APISIX Admin API is working!")
        print(f"   Routes found: {route_count}")
    else:
        print(f"   ✗ Unexpected status code: {response.status_code}")
except Exception as e:
    print(f"   ✗ Error: {e}")

# Test 2: Flask App
print("\n[Test 2] Testing Flask App (http://localhost:5000)...")
try:
    response = requests.get("http://localhost:5000/api/v1/routes", timeout=5)
    print(f"   Status: {response.status_code}")
    if response.status_code == 200:
        print(f"   ✓ Flask App is working!")
        print(f"   Response: {response.text[:200]}")
    else:
        print(f"   ! Status {response.status_code}: {response.text[:200]}")
except requests.exceptions.ConnectionError:
    print(f"   ✗ Connection refused - Flask app may not be running")
except Exception as e:
    print(f"   ✗ Error: {e}")

# Test 3: Additional Flask endpoints
print("\n[Test 3] Testing Flask Blacklist endpoint...")
try:
    response = requests.get("http://localhost:5000/api/Blacklist", timeout=5)
    print(f"   Status: {response.status_code}")
    if response.status_code == 200:
        print(f"   ✓ Blacklist endpoint OK")
        print(f"   Data: {response.text[:100]}")
except Exception as e:
    print(f"   ✗ Error: {e}")

# Test 4: Docker containers status
print("\n[Test 4] Docker Containers Status...")
import subprocess
try:
    result = subprocess.run(
        ["docker", "ps", "--format", "{{.Names}}\\t{{.Status}}"],
        capture_output=True,
        text=True,
        timeout=10
    )
    if result.returncode == 0:
        containers = [line for line in result.stdout.split('\n') if 'milk-api-manager' in line]
        if containers:
            print(f"   ✓ Found {len(containers)} containers:")
            for container in containers:
                print(f"     - {container}")
        else:
            print("   ! No milk-api-manager containers found")
    else:
        print(f"   ✗ Docker command failed")
except Exception as e:
    print(f"   ✗ Error: {e}")

print("\n" + "=" * 60)
print("Test Summary Complete")
print("=" * 60)
