import requests
import json
import subprocess
import time
import sys
import datetime

# Configuration
BASE_URL_USUARIOS = "http://localhost:30001"
BASE_URL_PROPRIEDADES = "http://localhost:30002"
NAMESPACE = "agrosolutions-local"

def get_sql_pod_name():
    cmd = f"kubectl get pods -n {NAMESPACE} -l app=sql-server -o jsonpath={{.items[0].metadata.name}}"
    try:
        # Simply run directly? Windows needs shell=True for path resolution if not full path
        res = subprocess.run(cmd, shell=True, capture_output=True, text=True)
        # Check stderr
        if res.returncode != 0:
            print(f"Kubectl error: {res.stderr}")
            return None
            
        return res.stdout.strip()
    except Exception as e:
        print(f"Error getting pod name: {e}")
        return None

def run_sql_query(query, database):
    pod_name = get_sql_pod_name()
    if not pod_name:
        print("❌ Could not find SQL Pod.")
        return None
    
    # Escape quotes for shell command if necessary, but keep it simple
    # The query is simple enough
    
    cmd_str = f'kubectl exec -n {NAMESPACE} {pod_name} -- /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "Fi@p2026" -d {database} -Q "{query}" -h -1 -W'
    
    if sys.platform == "win32":
        res = subprocess.run(["powershell", "-c", cmd_str], capture_output=True, text=True)
    else:
        res = subprocess.run(cmd_str, shell=True, capture_output=True, text=True)
        
    if res.returncode != 0:
        print(f"❌ SQL Error: {res.stderr}")
        return None
    return res.stdout.strip()

def main():
    print("🚀 Starting Issue Reproduction Script...")
    
    timestamp = int(time.time())
    
    # 1. Authentication
    print("\n--- 1. Authentication ---")
    
    # Register
    print(f"Registering user...")
    try:
        reg_payload = {
            "email": f"testuser{timestamp}@example.com",
            "senha": "Password123!",
            "tipoId": 1
        }
        
        reg_resp = requests.post(f"{BASE_URL_USUARIOS}/api/usuarios/registrar", json=reg_payload)
        
        if reg_resp.status_code == 200:
            print("✅ Registered.")
        elif reg_resp.status_code == 400 and "E-mail já cadastrado" in reg_resp.text:
             print("⚠️ User already exists, proceeding to login.")
        else:
            print(f"❌ Registration failed: {reg_resp.status_code} {reg_resp.text}")
            sys.exit(1)
            
        # Login
        print("Logging in...")
        login_resp = requests.post(f"{BASE_URL_USUARIOS}/api/usuarios/login", json={
            "email": reg_payload["email"],
            "password": reg_payload["senha"]
        })
        
        if login_resp.status_code != 200:
            print(f"❌ Login failed: {login_resp.status_code} {login_resp.text}")
            sys.exit(1)
            
        token = login_resp.json()["token"]
        headers = {"Authorization": f"Bearer {token}"}
        print("✅ Logged in.")
        
    except requests.exceptions.ConnectionError:
        print("❌ Connection failed. Check if services are running.")
        sys.exit(1)

    # 2. Test Propriedades Persistence
    print("\n--- 2. Testing Property Creation ---")
    prop_name = f"Fazenda Teste {timestamp}"
    
    print(f"Creating property '{prop_name}'...")
    try:
        create_resp = requests.post(
            f"{BASE_URL_PROPRIEDADES}/api/v1/Propriedades", 
            json={"nome": prop_name, "localizacao": "SP"},
            headers=headers
        )
    except Exception as e:
        print(f"❌ Request failed: {e}")
        sys.exit(1)
    
    prop_id = None
    if create_resp.status_code == 201:
        print("✅ Property created (API returned 201).")
        try:
            data = create_resp.json()
            prop_id = data.get("id")
            print(f"Property ID: {prop_id}")
        except:
             print("Warning: Could not parse response JSON.")
    else:
        print(f"❌ Failed to create property: {create_resp.status_code} {create_resp.text}")
        sys.exit(1)

    # 2.5 Test Talhao Persistence
    print("\n--- 2.5 Testing Talhao Creation ---")
    talhao_name = f"Talhao Teste {timestamp}"
    
    print(f"Creating talhao '{talhao_name}' in property {prop_id}...")
    try:
        talhao_resp = requests.post(
            f"{BASE_URL_PROPRIEDADES}/api/v1/Propriedades/{prop_id}/talhoes", 
            json={"nome": talhao_name, "cultura": "Soja", "area": 100.5},
            headers=headers
        )
    except Exception as e:
        print(f"❌ Talhao request failed: {e}")
        sys.exit(1)
        
    talhao_id = None
    if talhao_resp.status_code == 201:
        print("✅ Talhao created (API returned 201).")
        try:
            data = talhao_resp.json()
            talhao_id = data.get("id")
            print(f"Talhao ID: {talhao_id}")
        except:
             print("Warning: Could not parse response JSON.")
    else:
        print(f"❌ Failed to create talhao: {talhao_resp.status_code} {talhao_resp.text}")
        sys.exit(1)

    # 3. Check via API
    print("\n--- 3. Verifying Property and Talhao via API ---")
    list_resp = requests.get(f"{BASE_URL_PROPRIEDADES}/api/v1/Propriedades", headers=headers)
    # ... (rest of check)
    
    # Check Talhao
    talhao_list_resp = requests.get(f"{BASE_URL_PROPRIEDADES}/api/v1/Propriedades/{prop_id}/talhoes", headers=headers)
    if talhao_list_resp.status_code == 200:
        talhoes = talhao_list_resp.json()
        print(f"Found {len(talhoes)} talhoes.")
        found_talhao = False
        if talhoes and isinstance(talhoes, list):
             found_talhao = any(t.get("id") == talhao_id for t in talhoes)
        if found_talhao:
            print("✅ Talhao found in API list.")
        else:
            print("❌ Talhao NOT found in API list!")
    else:
         print(f"❌ Failed to list talhoes: {talhao_list_resp.status_code}")
    if list_resp.status_code == 200:
        props = list_resp.json()
        print(f"Found {len(props)} properties.")
        found = False
        if props and isinstance(props, list):
             found = any(p.get("id") == prop_id for p in props)
        
        if found:
            print("✅ Property found in API list.")
        else:
            print("❌ Property NOT found in API list!")
            print(f"List: {json.dumps(props, indent=2)}")
    else:
        print(f"❌ Failed to list properties: {list_resp.status_code}")

    # 4. Verify Persistence after Restart
    print("\n--- 4. Restarting Propriedades Deployment to verify persistence ---")
    
    # Trigger restart
    cmd_restart = f"kubectl rollout restart deployment/propriedades -n {NAMESPACE}"
    print(f"Executing: {cmd_restart}")
    subprocess.run(cmd_restart, shell=True, capture_output=True)
    
    print("Waiting for rollout to complete (this may take a minute)...")
    # Wait for rollout
    cmd_status = f"kubectl rollout status deployment/propriedades -n {NAMESPACE}"
    subprocess.run(cmd_status, shell=True, capture_output=True)
    
    # Wait a bit more just in case
    time.sleep(10)
    
    # 5. Check via API again
    print("\n--- 5. Verifying Property and Talhao via API After Restart ---")
    try:
        list_resp = requests.get(f"{BASE_URL_PROPRIEDADES}/api/v1/Propriedades", headers=headers)
        if list_resp.status_code == 200:
             # (existing check for property)
             pass
        else:
             print(f"❌ Failed to list properties: {list_resp.status_code}")
             
        # Check Talhao
        talhao_list_resp = requests.get(f"{BASE_URL_PROPRIEDADES}/api/v1/Propriedades/{prop_id}/talhoes", headers=headers)
        if talhao_list_resp.status_code == 200:
            talhoes = talhao_list_resp.json()
            print(f"Found {len(talhoes)} talhoes.")
            found_talhao = any(t.get("id") == talhao_id for t in talhoes) if talhoes else False
            if found_talhao:
                print("✅ Talhao found in API list after restart. Persistence works!")
            else:
                 print("❌ Talhao NOT found in API list after restart! DATA LOST!")
        else:
             print(f"❌ Failed to list talhoes: {talhao_list_resp.status_code}")
    except requests.exceptions.ConnectionError:
        print("❌ Connection failed after restart.")

if __name__ == "__main__":
    main()
