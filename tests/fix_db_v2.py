import subprocess

NAMESPACE = "agrosolutions-local"
SQL_POD_SELECTOR = "app=sql-server"

def get_sql_pod_name():
    # Run kubectl directly
    cmd = ["kubectl", "get", "pods", "-n", NAMESPACE, "-l", SQL_POD_SELECTOR, "-o", "jsonpath={.items[0].metadata.name}"]
    res = subprocess.run(cmd, capture_output=True, text=True)
    if res.returncode != 0:
        print(f"Error finding pod: {res.stderr}")
        return None
    return res.stdout.strip()

def run_sql(query, database):
    pod_name = get_sql_pod_name()
    if not pod_name:
        print("❌ Could not find SQL Pod.")
        return
    
    print(f"Executing on {database}: {query}")
    
    k_args = [
        "kubectl", "exec", "-n", NAMESPACE, pod_name, "--",
        "/opt/mssql-tools18/bin/sqlcmd",
        "-C",
        "-S", "localhost",
        "-U", "sa",
        "-P", "Fi@p2026",
        "-d", database,
        "-Q", query,
        "-h", "-1"
    ]
    
    res = subprocess.run(k_args, capture_output=True, text=True)
    if res.returncode != 0:
        print(f"❌ Error: {res.stderr}")
        print(f"Output: {res.stdout}")
    else:
        print(f"✅ Result: {res.stdout}")

def main():
    print("🔧 Fixing Database Seeds...")
    
    # Fix TiposUsuarios
    insert_sql = "SET IDENTITY_INSERT TiposUsuarios ON; IF NOT EXISTS (SELECT * FROM TiposUsuarios WHERE Id = 1) INSERT INTO TiposUsuarios (Id, Descricao) VALUES (1, 'Produtor'); SET IDENTITY_INSERT TiposUsuarios OFF;"
    run_sql(insert_sql, "AgroSolutionsUsuarios")
    
    insert_sql_2 = "SET IDENTITY_INSERT TiposUsuarios ON; IF NOT EXISTS (SELECT * FROM TiposUsuarios WHERE Id = 2) INSERT INTO TiposUsuarios (Id, Descricao) VALUES (2, 'Admin'); SET IDENTITY_INSERT TiposUsuarios OFF;"
    run_sql(insert_sql_2, "AgroSolutionsUsuarios")

if __name__ == "__main__":
    main()
