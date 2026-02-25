import requests
import json
import random
import time
import sys
import os

# Configuration (adjust ports if necessary based on qa_validation_v2.py)
BASE_URL_USUARIOS = "http://localhost:30001"
BASE_URL_PROPRIEDADES = "http://localhost:30002"
BASE_URL_INGESTAO = "http://localhost:30003"

OUTPUT_FILE = "simulation_data.json"

def get_unique_suffix():
    return f"{int(time.time())}_{random.randint(1000, 9999)}"

def create_user(index):
    suffix = get_unique_suffix()
    email = f"user_{index}_{suffix}@example.com"
    password = "Password123!"
    
    print(f"Creating user: {email}")
    payload = {
        "email": email,
        "senha": password,
        "tipoId": 1 # Assuming 1 is producer/admin
    }
    
    try:
        # Based on UsuariosController: [Route("api/usuarios")]
        resp = requests.post(f"{BASE_URL_USUARIOS}/api/usuarios/registrar", json=payload)
        if resp.status_code in [200, 201]:
            # This endpoint returns { "mensagem": "..." } not full user data, but that's fine.
            return { "email": email, "password": password }
        else:
            print(f"Failed to create user {email}: {resp.status_code} - {resp.text}")
            return None
    except Exception as e:
        print(f"Exception creating user: {e}")
        return None

def login_user(email, password):
    print(f"Logging in user: {email}")
    payload = {
        "email": email,
        "password": password
    }
    try:
        # Based on UsuariosController: [HttpPost("login")]
        resp = requests.post(f"{BASE_URL_USUARIOS}/api/usuarios/login", json=payload)
        if resp.status_code == 200:
            return resp.json().get("token")
        else:
            print(f"Failed to login {email}: {resp.status_code} - {resp.text}")
            return None
    except Exception as e:
        print(f"Exception logging in: {e}")
        return None

def create_property(token, user_index, prop_index):
    suffix = get_unique_suffix()
    nome = f"Propriedade {user_index}-{prop_index} {suffix}"
    localizacao = f"Localizacao {user_index}-{prop_index}"
    
    print(f"Creating property: {nome}")
    # PropriedadesController: [HttpPost] Route("api/v1/[controller]") -> /api/v1/Propriedades
    # DTO: CreatePropriedadeDto(string Nome, string Localizacao)
    payload = {
        "nome": nome,
        "localizacao": localizacao
    }
    
    headers = {"Authorization": f"Bearer {token}"}
    
    try:
        resp = requests.post(f"{BASE_URL_PROPRIEDADES}/api/v1/propriedades", json=payload, headers=headers)
        if resp.status_code in [200, 201]:
            return resp.json()
        else:
            print(f"Failed to create property {nome}: {resp.status_code} - {resp.text}")
            return None
    except Exception as e:
        print(f"Exception creating property: {e}")
        return None

def create_field(token, property_id, user_index, prop_index, field_index):
    suffix = get_unique_suffix()
    nome = f"Talhão {user_index}-{prop_index}-{field_index} {suffix}"
    area = float(random.randint(10, 50))
    cultura = "Soja" if random.random() > 0.5 else "Milho"
    
    print(f"Creating field: {nome} for property {property_id}")
    # PropriedadesController: [HttpPost("{id}/talhoes")] -> /api/v1/Propriedades/{id}/talhoes
    # DTO: CreateTalhaoDto(string Nome, string Cultura, decimal Area)
    payload = {
        "nome": nome,
        "cultura": cultura,
        "area": area
    }
    
    headers = {"Authorization": f"Bearer {token}"}
    
    try:
        resp = requests.post(f"{BASE_URL_PROPRIEDADES}/api/v1/propriedades/{property_id}/talhoes", json=payload, headers=headers)
        if resp.status_code in [200, 201]:
            return resp.json()
        else:
            print(f"Failed to create field {nome}: {resp.status_code} - {resp.text}")
            return None
    except Exception as e:
        print(f"Exception creating field: {e}")
        return None


def main():
    print("🚀 Starting Data Seeder...")
    
    simulation_data = []

    for u in range(1, 6): # 5 Users
        user = create_user(u)
        if not user:
            continue
            
        token = login_user(user['email'], user['password'])
        if not token:
            continue
            
        user_record = {
            "email": user['email'],
            "password": user['password'],
            "token": token,
            "properties": []
        }
        
        for p in range(1, 5): # 4 Properties
            prop = create_property(token, u, p)
            if not prop:
                continue
            
            prop_record = {
                "id": prop['id'],
                "nome": prop['nome'],
                "talhoes": []
            }
            
            for t in range(1, 4): # 3 Fields (Talhões)
                field = create_field(token, prop['id'], u, p, t)
                if field:
                    prop_record['talhoes'].append(field['id'])
            
            user_record['properties'].append(prop_record)
        
        simulation_data.append(user_record)

    # Save to file
    with open(OUTPUT_FILE, 'w') as f:
        json.dump(simulation_data, f, indent=2)
    
    print(f"\n✅ Data seeding complete. Configuration saved to {OUTPUT_FILE}")
    print(f"Created {len(simulation_data)} users with properties and fields.")

if __name__ == "__main__":
    main()
