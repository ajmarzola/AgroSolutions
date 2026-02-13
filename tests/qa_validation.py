import requests
import json
import subprocess
import time
import sys
import datetime

# Configuration
BASE_URL_USUARIOS = "http://localhost:30001"
BASE_URL_PROPRIEDADES = "http://localhost:30002"
BASE_URL_INGESTAO = "http://localhost:30003"
SQL_POD_SELECTOR = "app=sql-server"
NAMESPACE = "agrosolutions-local"

def get_sql_pod_name():
    cmd = f"kubectl get pods -n {NAMESPACE} -l {SQL_POD_SELECTOR} -o jsonpath={{.items[0].metadata.name}}"
    res = subprocess.run(["powershell", "-c", cmd], capture_output=True, text=True)
    return res.stdout.strip()

def run_sql_query(query, database):
    pod_name = get_sql_pod_name()
    if not pod_name:
        print("❌ Could not find SQL Pod.")
        return None
    
    # Using sqlcmd inside the pod
    # Password set in .env is Fi@p2026 (updated in configmap patch)
    
    # Note: Use single quotes for PowerShell execution string, double quotes for query
    cmd_str = f'kubectl exec -n {NAMESPACE} {pod_name} -- /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "Fi@p2026" -d {database} -Q "{query}" -h -1 -W'
    
    # print(f"Executing SQL on {database}...")
    res = subprocess.run(["powershell", "-c", cmd_str], capture_output=True, text=True)
    if res.returncode != 0:
        print(f"❌ SQL Error: {res.stderr}")
        return None
    return res.stdout.strip()

def main():
    print("🚀 Starting QA Validation Script...")
    
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
    except Exception as e:
        print(f"❌ Login failed: {e}")
        sys.exit(1)

    headers = {"Authorization": f"Bearer {token}"}

    # 2. Properties & Talhoes
    print("\n--- 2. Properties & Talhoes ---")
    prop_id = None
    talhao_id = None
    try:
        # Create Property
        prop_payload = {"nome": "QA Farm", "localizacao": "QA Lab"}
        prop_resp = requests.post(f"{BASE_URL_PROPRIEDADES}/api/v1/Propriedades", json=prop_payload, headers=headers)
        if prop_resp.status_code not in [200, 201]:
            print(f"❌ Create Property failed: {prop_resp.status_code} {prop_resp.text}")
            sys.exit(1)
        
        prop_data = prop_resp.json()
        prop_id = prop_data.get("id")
        print(f"✅ Property Created: {prop_id}")
        
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
        initial_leituras = run_sql_query("SELECT COUNT(*) FROM Leituras", "AgroSolutionsIngestao")
        print(f"Initial Leituras Count: {initial_leituras}")
        
        # Send Reading
        metricas = {
            "umidadeSoloPercentual": 25, # < 30% to trigger low humidity alert
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
        if ingest_resp.status_code not in [200, 201]:
            print(f"❌ Ingestion failed: {ingest_resp.status_code} {ingest_resp.text}")
            sys.exit(1)
        print("✅ Reading sent successfully.")
        
        # Validate Persistence
        time.sleep(2) # Wait for persistence
        final_leituras = run_sql_query("SELECT COUNT(*) FROM Leituras", "AgroSolutionsIngestao")
        print(f"Final Leituras Count: {final_leituras}")
        
        if final_leituras and initial_leituras and int(final_leituras) > int(initial_leituras):
             print("✅ DB Validation: Data persisted in AgroSolutionsIngestao.")
        else:
             print("❌ DB Validation: Count did not increase or invalid response.")
             
        # Validate Alert Generation
        print("Waiting for alert generation (Async)...")
        time.sleep(10) # Give RabbitMQ and Analise worker time to process.
        
        # Check Alertas table in AgroSolutionsAnalise
        alerts_count = run_sql_query("SELECT COUNT(*) FROM Alertas", "AgroSolutionsAnalise")
        print(f"Alerts Count: {alerts_count}") 
        
        if alerts_count and int(alerts_count) > 0:
            print("✅ Alert Validation: Alert found is database.")
        else:
            print("⚠️ Alert Validation: No alerts found.")
            
    except Exception as e:
        print(f"❌ Ingestion/Alert flow failed: {e}")
        sys.exit(1)

    print("\n--- QA VALIDATION COMPLETE ---")

if __name__ == "__main__":
    main()
