import subprocess

NAMESPACE = "agrosolutions-local"
SQL_POD_SELECTOR = "app=sql-server"

def get_sql_pod_name():
    cmd = f"kubectl get pods -n {NAMESPACE} -l {SQL_POD_SELECTOR} -o jsonpath={{.items[0].metadata.name}}"
    res = subprocess.run(["powershell", "-c", cmd], capture_output=True, text=True)
    return res.stdout.strip()

def run_sql(query, database):
    pod_name = get_sql_pod_name()
    if not pod_name:
        print("❌ Could not find SQL Pod.")
        return
    
    print(f"Executing on {database}: {query}")
    cmd_str = f'kubectl exec -n {NAMESPACE} {pod_name} -- /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Fi@p2026" -d {database} -Q "{query}" -h -1'
    res = subprocess.run(["powershell", "-c", cmd_str], capture_output=True, text=True)
    if res.returncode != 0:
        print(f"❌ Error: {res.stderr}")
    else:
        print(f"✅ Result: {res.stdout}")

def main():
    print("🔧 Fixing Database Seeds...")
    # Fix TiposUsuarios
    # Check if exists first
    check = "SELECT Count(*) FROM TiposUsuarios WHERE Id = 1"
    run_sql(check, "AgroSolutionsUsuarios") 
    
    # Insert
    # Assuming columns: Id, Descricao (or similar). I'll guess 'Descricao'.
    # If fails, I'll read the schema.
    insert_sql = "IF NOT EXISTS (SELECT * FROM TiposUsuarios WHERE Id = 1) INSERT INTO TiposUsuarios (Id, Descricao) VALUES (1, 'Produtor')"
    run_sql(insert_sql, "AgroSolutionsUsuarios")
    
    insert_sql_2 = "IF NOT EXISTS (SELECT * FROM TiposUsuarios WHERE Id = 2) INSERT INTO TiposUsuarios (Id, Descricao) VALUES (2, 'Admin')"
    run_sql(insert_sql_2, "AgroSolutionsUsuarios")

if __name__ == "__main__":
    main()
