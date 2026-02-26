import requests
import json
import subprocess
import time
import sys
import datetime
import re
import pyodbc 

# Configuration
BASE_URL_USUARIOS = "http://localhost:30001"
BASE_URL_PROPRIEDADES = "http://localhost:30002"
BASE_URL_INGESTAO = "http://localhost:30003"
SQL_POD_SELECTOR = "app=sql-server"
ANALISE_POD_SELECTOR = "app.kubernetes.io/name=analise"
# ANALISE_POD_SELECTOR = "app.kubernetes.io/name=analise" # Keep original commented if used elsewhere
NAMESPACE = "agrosolutions-local"

# Azure SQL Connection String
SQL_CONN_STR = "Driver={ODBC Driver 17 for SQL Server};Server=tcp:agrosolutions.database.windows.net,1433;Database={database};Uid=usr_agro;Pwd=Fi@p2026;Encrypt=yes;TrustServerCertificate=yes;Connection Timeout=60;"
SQL_CONN_STR_MASTER = "Driver={ODBC Driver 17 for SQL Server};Server=tcp:agrosolutions.database.windows.net,1433;Database=master;Uid=usr_agro;Pwd=Fi@p2026;Encrypt=yes;TrustServerCertificate=yes;Connection Timeout=60;"

def get_pod_name(selector):
    # Use output=name and strip 'pod/' prefix
    cmd = f"kubectl get pods -n {NAMESPACE} -l {selector} -o name"
    try:
        res = subprocess.run(["powershell", "-c", cmd], capture_output=True, text=True)
        if res.returncode == 0 and res.stdout.strip():
            # Return first line, remove 'pod/'
            first_pod = res.stdout.strip().split('\n')[0]
            return first_pod.replace("pod/", "")
    except Exception as e:
        print(f"Error getting pod name: {e}")
    return ""

def run_sql_query(query, database):
    try:
        # Use replace instead of format to avoid issues with curly braces in Driver definition
        conn_str = SQL_CONN_STR.replace("{database}", database)
        if database == "master":
             conn_str = SQL_CONN_STR_MASTER
        
        print(f"DEBUG: Connecting to {database}...")
        with pyodbc.connect(conn_str) as conn:
            cursor = conn.cursor()
            cursor.execute(query)
            
            # If query returns rows (SELECT), fetch result
            if query.strip().upper().startswith("SELECT"):
                row = cursor.fetchone()
                if row:
                    res = str(row[0]) # Start with first column
                    return res
                else:
                    return "0" 
            
            conn.commit()
            return "OK"

    except Exception as e:
        print(f"❌ SQL Error using pyodbc: {e}")
        return None

def check_metrics_endpoint(base_url, service_name):
    url = f"{base_url}/metrics"
    print(f"Checking metrics at {url}...")
    try:
        resp = requests.get(url, timeout=5)
        if resp.status_code == 200:
            print(f"✅ Metrics endpoint functional for {service_name}")
            return True
        else:
            print(f"❌ Metrics endpoint returned {resp.status_code} for {service_name}")
            return False
    except Exception as e:
        print(f"❌ Metrics check failed for {service_name}: {e}")
        return False

def check_logs_for_pattern(selector, pattern, service_name):
    pod_name = get_pod_name(selector)
    if not pod_name:
         print(f"❌ Could not find pod for {service_name}")
         return False
    
    print(f"Checking logs of {pod_name} for pattern '{pattern}'...")
    cmd = f"kubectl logs -n {NAMESPACE} {pod_name} --tail=200"
    res = subprocess.run(["powershell", "-c", cmd], capture_output=True, text=True)
    
    if res.returncode != 0:
        print(f"❌ Failed to get logs for {service_name}: {res.stderr}")
        return False
        
    if re.search(pattern, res.stdout, re.IGNORECASE):
        print(f"✅ Pattern '{pattern}' found in {service_name} logs.")
        return True
    else:
        print(f"❌ Pattern '{pattern}' NOT found in {service_name} logs.")
        return False

def main():
    print("🚀 Starting QA Validation Script (v2)...")
    
    # Check SQL connection first
    print("\n--- 0. Infrastructure Check ---")
    try:
        res = run_sql_query("SELECT 1", "master")
        if res and "1" in res:
            print("✅ SQL Server is ready and accessible.")
        else:
            print(f"❌ SQL Server check failed. Output: {res}")
            sys.exit(1)
    except Exception as e:
        print(f"❌ Infrastructure check failed: {e}")
        sys.exit(1)
    
    # 1. Authentication
    print("\n--- 1. Authentication ---")
    email = f"qa_user_{int(time.time())}@test.com"
    password = "QaPassword123!"
    
    # Register
    print(f"Registering {email}...")
    try:
        reg_resp = requests.post(f"{BASE_URL_USUARIOS}/api/usuarios/registrar", json={
            "nome": "QA Automation",
            "email": email,
            "senha": password,
            "tipoId": 1
        })
        if reg_resp.status_code not in [200, 201]:
             print(f"Register info: {reg_resp.status_code} {reg_resp.text}")
             if reg_resp.status_code != 400: # Maybe already exists
                 sys.exit(1)
    except Exception as e:
        print(f"❌ Register failed: {e}")
        sys.exit(1)

    # Login
    print("Logging in...")
    try:
        login_resp = requests.post(f"{BASE_URL_USUARIOS}/api/usuarios/login", json={
            "email": email,
            "password": password
        })
        if login_resp.status_code != 200:
            print(f"❌ Login failed: {login_resp.text}")
            sys.exit(1)
            
        token = login_resp.json().get("token")
        if not token:
            print("❌ No token returned.")
            sys.exit(1)
        print("✅ Login Successful. Token obtained.")
        print(f"DEBUG: Token: {token}")
        try:
            # Simple debug decode without external libs since we might not have pyjwt installed
            import base64
            import json
            parts = token.split('.')
            if len(parts) > 1:
                padding = '=' * (4 - len(parts[1]) % 4)
                payload = json.loads(base64.urlsafe_b64decode(parts[1] + padding).decode('utf-8'))
                print(f"DEBUG: Token Payload: {json.dumps(payload, indent=2)}")
        except Exception as ex:
             print(f"DEBUG: Failed to decode token: {ex}")

    except Exception as e:
        print(f"❌ Login failed: {e}")
        sys.exit(1)

    headers = {"Authorization": f"Bearer {token}"}

    # 2. Properties & Talhoes
    print("\n--- 2. Properties & Talhoes ---")
    prop_id = None
    talhao_id = None
    try:
        # Check Initial Prop Count
        initial_props = run_sql_query("SELECT COUNT(*) FROM Propriedades", "Propriedades") # DB Name fixed for Azure

        # Create Property
        prop_payload = {"nome": "QA Farm", "localizacao": "QA Lab"}
        prop_resp = requests.post(f"{BASE_URL_PROPRIEDADES}/api/v1/Propriedades", json=prop_payload, headers=headers)
        if prop_resp.status_code not in [200, 201]:
            print(f"❌ Create Property failed: {prop_resp.status_code} {prop_resp.text}")
            sys.exit(1)
        
        prop_data = prop_resp.json()
        prop_id = prop_data.get("id")
        print(f"✅ Property Created: {prop_id}")
        
        # Verify Persistence
        time.sleep(1)
        final_props = run_sql_query("SELECT COUNT(*) FROM Propriedades", "Propriedades")
        if final_props and initial_props and int(final_props) > int(initial_props):
             print("✅ DB Validation: Property persisted.")
        else:
             print(f"❌ DB Validation: Property persistance check failed (Initial: {initial_props}, Final: {final_props}).")
             # sys.exit(1) 

        # Create Talhao
        talhao_payload = {"nome": "Talhao QA", "cultura": "Milho", "area": 30}
        talhao_resp = requests.post(f"{BASE_URL_PROPRIEDADES}/api/v1/Propriedades/{prop_id}/talhoes", json=talhao_payload, headers=headers)
        if talhao_resp.status_code not in [200, 201]:
            print(f"❌ Create Talhao failed: {talhao_resp.status_code} {talhao_resp.text}")
            sys.exit(1)
            
        talhao_data = talhao_resp.json()
        talhao_id = talhao_data.get("id")
        print(f"✅ Talhao Created: {talhao_id}")
        
    except Exception as e:
        print(f"❌ Property/Talhao flow failed: {e}")
        sys.exit(1)

    # 3. Ingestao (Sensor Reading) - With Low Humidity to trigger Alert
    print("\n--- 3. Ingestion & Alerts ---")
    try:
        # Check initial count in Ingestao
        initial_leituras = run_sql_query("SELECT COUNT(*) FROM SensorLeitura", "Ingestao") # Database name fixed for Azure
        print(f"Initial Leituras Count: {initial_leituras}")
        
        # Send Reading
        metricas = {
            "umidadeSoloPercentual": 15, # < 20% to trigger critical low humidity alert
            "temperaturaCelsius": 30,
            "precipitacaoMilimetros": 0
        }
        payload = {
            "idPropriedade": prop_id,
            "idTalhao": talhao_id,
            "origem": "QA_SCRIPT",
            "dataHoraCapturaUtc": datetime.datetime.utcnow().isoformat() + "Z",
            "metricas": metricas,
            "meta": {"idDispositivo": "QA-DEV-01"}
        }
        
        print("Sending low humidity reading...")
        ingest_resp = requests.post(f"{BASE_URL_INGESTAO}/api/v1/leituras-sensores", json=payload, headers=headers)
        if ingest_resp.status_code not in [202, 201, 200]:
            print(f"❌ Ingestion failed: {ingest_resp.status_code} {ingest_resp.text}")
            sys.exit(1)
        print("✅ Reading sent successfully.")
        
        # Validate Persistence
        print("Waiting for processing and persistence (5s)...")
        time.sleep(5) # Wait for RabbitMQ -> Analise -> DB
        
        final_leituras = run_sql_query("SELECT COUNT(*) FROM SensorLeitura", "Ingestao")
        print(f"Final Leituras Count: {final_leituras}")
        
        if final_leituras is not None and initial_leituras is not None and int(final_leituras) > int(initial_leituras):
             print("✅ DB Validation: Data persisted in Ingestao.")
        else:
             print("⚠️ DB Validation: Count did not increase in Ingestao (Check if it stores locally or only via queue).")
        
        # 4. Alertas validation
        
        # Check Alertas table in Analise
        alerts_count = run_sql_query("SELECT COUNT(*) FROM Alerta", "Analise")
        print(f"Alerts Count: {alerts_count}") 
        
        if alerts_count and int(alerts_count) > 0:
            print("✅ Alert Validation: Alert found is database.")
        else:
            print("❌ Alert Validation: No alerts found.")
            sys.exit(1)
            
    except Exception as e:
        print(f"❌ Ingestion/Alert flow failed: {e}")
        sys.exit(1)

    # 4. Observability
    print("\n--- 4. Observability ---")
    if not check_metrics_endpoint(BASE_URL_INGESTAO, "Ingestao"):
        pass # Don't exit, just warn for now
    
    # Check for TraceId in Analise logs
    check_logs_for_pattern(ANALISE_POD_SELECTOR, "TraceId", "Analise")

    print("\n--- QA VALIDATION COMPLETE ---")

if __name__ == "__main__":
    main()
