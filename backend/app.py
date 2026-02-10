from flask import Flask, request, jsonify
import requests
import os

app = Flask(__name__)

# APISIX Admin API Config
# Use localhost if running locally, or environment variable
APISIX_ADMIN_KEY = os.getenv("APISIX_ADMIN_KEY", "edd1c9f034335f136f87ad84b625c8f1")
APISIX_ADMIN_URL = os.getenv("APISIX_ADMIN_URL", "http://localhost:9180/apisix/admin")

@app.route('/api/v1/routes', methods=['GET'])
def get_routes():
    try:
        resp = requests.get(f"{APISIX_ADMIN_URL}/routes", headers={"X-API-KEY": APISIX_ADMIN_KEY})
        return jsonify(resp.json()), resp.status_code
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/api/Blacklist', methods=['GET'])
def get_blacklist():
    try:
        resp = requests.get(f"{APISIX_ADMIN_URL}/plugin_metadata/traffic-blocker", headers={"X-API-KEY": APISIX_ADMIN_KEY})
        if resp.status_code == 404:
            return jsonify([]), 200
        
        data = resp.json()
        blacklist = data.get('value', {}).get('blacklist', [])
        return jsonify(blacklist), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/api/Blacklist', methods=['POST'])
def update_blacklist():
    data = request.json
    ip = data.get('ip')
    action = data.get('action', 'add') # add | remove
    
    if not ip:
        return jsonify({"error": "IP is required"}), 400

    try:
        # Fetch current metadata
        meta_resp = requests.get(f"{APISIX_ADMIN_URL}/plugin_metadata/traffic-blocker", headers={"X-API-KEY": APISIX_ADMIN_KEY})
        
        if meta_resp.status_code == 404:
            metadata_value = {"blacklist": []}
        else:
            metadata_value = meta_resp.json().get('value', {"blacklist": []})
        
        blacklist = set(metadata_value.get('blacklist', []))
        if action == 'add':
            blacklist.add(ip)
        elif action == 'remove':
            blacklist.discard(ip)
            
        # Update metadata
        update_resp = requests.put(
            f"{APISIX_ADMIN_URL}/plugin_metadata/traffic-blocker",
            headers={"X-API-KEY": APISIX_ADMIN_KEY},
            json={"blacklist": list(blacklist)}
        )
        return jsonify({"message": f"IP {ip} {action}ed successfully"}), update_resp.status_code
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/api/Consumer', methods=['GET'])
def get_consumers():
    try:
        resp = requests.get(f"{APISIX_ADMIN_URL}/consumers", headers={"X-API-KEY": APISIX_ADMIN_KEY})
        raw_data = resp.json()
        
        # Transform APISIX consumer structure to our model
        consumers = []
        # APISIX returns a list of items inside 'list' or similar depending on version/format
        # Assuming typical APISIX response format
        for item in raw_data.get('list', []):
            value = item.get('value', {})
            plugins = value.get('plugins', {})
            limit_count = plugins.get('limit-count', {})
            
            consumers.append({
                "username": value.get('username'),
                "desc": value.get('desc', ""),
                "labels": value.get('labels', []),
                "quota": {
                    "count": limit_count.get('count', 1000),
                    "time_window": limit_count.get('time_window', 3600),
                    "rejected_code": limit_count.get('rejected_code', 429),
                    "rejected_msg": limit_count.get('rejected_msg', "API quota exceeded.")
                }
            })
        return jsonify(consumers), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/api/Consumer', methods=['POST'])
def update_consumer():
    data = request.json
    username = data.get('username')
    if not username:
        return jsonify({"error": "Username is required"}), 400
    
    quota = data.get('quota', {})
    
    consumer_config = {
        "username": username,
        "desc": data.get('desc', ""),
        "labels": data.get('labels', []),
        "plugins": {
            "limit-count": {
                "count": quota.get('count', 1000),
                "time_window": quota.get('time_window', 3600),
                "rejected_code": quota.get('rejected_code', 429),
                "rejected_msg": quota.get('rejected_msg', "API quota exceeded.")
            }
        }
    }

    try:
        resp = requests.put(
            f"{APISIX_ADMIN_URL}/consumers/{username}",
            headers={"X-API-KEY": APISIX_ADMIN_KEY},
            json=consumer_config
        )
        return jsonify({"message": "Consumer updated successfully"}), resp.status_code
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/api/Consumer/<username>', methods=['DELETE'])
def delete_consumer(username):
    try:
        resp = requests.delete(f"{APISIX_ADMIN_URL}/consumers/{username}", headers={"X-API-KEY": APISIX_ADMIN_KEY})
        return jsonify({"message": "Consumer deleted successfully"}), resp.status_code
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
