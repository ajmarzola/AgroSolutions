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
DB_NAME = "AgroSolutionsPropriedades"

def get_sql_pod_name():
    cmd = f"kubectl get pods -n {NAMESPACE} -l app=sql-server --output name"
    try:
        if sys.platform == "win32":
            res = subprocess.run(["powershell", "-c", cmd], capture_output=True, text=True)
            if res.returncode != 0:
                 print(f"Error finding pod: {res.stderr}")
            # Output is pod/sql-server-xxx
            return res.stdout.strip().replace("pod/", "")
        else:
             res = subprocess.run(cmd, shell=True, capture_output=True, text=True)
             return res.stdout.strip().replace("pod/", "")
    except Exception as e:
        print(f"Error getting pod name: {e}")
        return None

def run_sql_query(query, database):
    pod_name = get_sql_pod_name()
    if not pod_name:
        print("❌ Could not find SQL Pod.")
        return None
    
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
    print("🚀 Starting Persistence Test Script (with DB Check)...")
    timestamp = int(time.time())
    
    # 1. Authentication
    print("\n--- 1. Authentication ---")
    reg_payload = {
        "email": f"testuser{timestamp}@example.com",
        "senha": "Password123!",
        "tipoId": 1
    }
    
    try:
        reg_resp = requests.post(f"{BASE_URL_USUARIOS}/api/usuarios/registrar", json={
            "email": reg_payload["email"],
            "senha": reg_payload["senha"],
            "tipoId": reg_payload["tipoId"]
        })
        
        if reg_resp.status_code == 200:
            print("✅ Registered.")
        elif reg_resp.status_code == 400 and "E-mail já cadastrado" in reg_resp.text:
             print("⚠️ User already exists.")
        else:
            print(f"❌ Registration failed: {reg_resp.text}")
            sys.exit(1)
        
        login_resp = requests.post(f"{BASE_URL_USUARIOS}/api/usuarios/login", json={
            "email": reg_payload["email"],
            "password": reg_payload["senha"]
        })
        
        if login_resp.status_code != 200:
            print(f"❌ Login failed: {login_resp.status_code}")
            sys.exit(1)
            
        token = login_resp.json()["token"]
        headers = {"Authorization": f"Bearer {token}"}
        print("✅ Logged in.")
        
    except requests.exceptions.ConnectionError:
        print("❌ Connection failed.")
        sys.exit(1)

    # 2. Create Property
    print("\n--- 2. Create Property ---")
    prop_name = f"Fazenda DB Check {timestamp}"
    create_resp = requests.post(
        f"{BASE_URL_PROPRIEDADES}/api/v1/Propriedades", 
        json={"nome": prop_name, "localizacao": "SP"},
        headers=headers
    )
    
    prop_id = None
    if create_resp.status_code == 201:
        data = create_resp.json()
        prop_id = data.get("id")
        print(f"✅ Property created. ID: {prop_id}")
    else:
        print(f"❌ Failed to create property: {create_resp.status_code}")
        print(create_resp.text)
        sys.exit(1)

    # 3. Check Database
    print(f"\n--- 3. Verifying Property in Database '{DB_NAME}' ---")
    sql_check = f"SELECT Id, Nome FROM Propriedades WHERE Id = '{prop_id}'"
    print(f"Running SQL: {sql_check}")
    db_result = run_sql_query(sql_check, DB_NAME)
    
    if db_result and prop_id.lower() in db_result.lower():
        print(f"✅ Property found in Database: {db_result}")
    else:
        print(f"❌ Property NOT found in Database! Result: '{db_result}'")

if __name__ == "__main__":
    main()
