# AgroSolutions -- Postman Collections

Este diretório contém as coleções do Postman para testar as APIs do
projeto **AgroSolutions** em ambiente local (Kubernetes via NodePort).

------------------------------------------------------------------------

## 📦 Estrutura Recomendada

    tools/postman/
      collections/
        AgroSolutions_Usuarios.postman_collection.json
        AgroSolutions_Propriedades.postman_collection.json
        AgroSolutions_Ingestao.postman_collection.json
        AgroSolutions_Analise.postman_collection.json
        AgroSolutions_HappyPath_Local.postman_collection.json
      environments/
        AgroSolutions_Local.postman_environment.example.json
        AgroSolutions_HappyPath_Local.postman_environment.example.json

------------------------------------------------------------------------

## 🚀 Pré-requisitos

Antes de executar as coleções:

1.  Cluster Kubernetes rodando

2.  Aplicar overlay local:

    ``` bash
    kubectl apply -k infra/k8s/overlays/local
    ```

3.  Confirmar que todos os pods estão `Running`:

    ``` bash
    kubectl get pods -n agrosolutions-local
    ```

As APIs devem estar expostas via NodePort:

  Serviço        Porta Local
  -------------- -------------
  Usuários       30001
  Propriedades   30002
  Ingestão       30003
  Análise        30004

------------------------------------------------------------------------

## 📥 Importando no Postman

1.  Abrir Postman
2.  Clique em **Import**
3.  Importar:
    -   As coleções em `collections/`
    -   O environment em `environments/`
4.  Selecionar o environment correspondente no canto superior direito

------------------------------------------------------------------------

## ▶️ Executando o Happy Path (Recomendado)

Use a coleção:

**AgroSolutions_HappyPath_Local.postman_collection.json**

Fluxo executado automaticamente:

1.  Registrar usuário
2.  Login (salva `jwt` automaticamente)
3.  Criar Propriedade (salva `propriedadeId`)
4.  Criar Talhão (salva `talhaoId`)
5.  Enviar Leitura de Sensor
6.  Consultar Leituras
7.  Consultar Alertas

### Observações

-   Se o usuário já existir, o passo de registro pode retornar `400` ou
    `409` (aceitável).
-   O token JWT é salvo automaticamente na variável `jwt`.
-   IDs criados são armazenados no environment para uso nos próximos
    requests.

------------------------------------------------------------------------

## 🧪 Testes Individuais

Você também pode utilizar as coleções separadas:

-   **Usuarios** → Login e gestão de usuários
-   **Propriedades** → CRUD de propriedades e talhões
-   **Ingestão** → Envio de leituras
-   **Análise** → Consulta de leituras e alertas

------------------------------------------------------------------------

## 🔐 Variáveis Importantes do Environment

  Variável        Descrição
  --------------- ---------------------------
  jwt             Token JWT gerado no login
  email           Email do usuário de teste
  password        Senha do usuário
  propriedadeId   ID criado dinamicamente
  talhaoId        ID criado dinamicamente

------------------------------------------------------------------------

## 🧭 Smoke Test Manual (Validação Rápida)

1.  Login → obter token
2.  Criar propriedade
3.  Criar talhão
4.  Enviar leitura com umidade \< 30%
5.  Consultar alertas após processamento

------------------------------------------------------------------------

## 📌 Observabilidade

Durante os testes você pode validar:

-   Métricas: `GET /metrics`
-   Health: `GET /health`
-   Traces: Jaeger
-   Logs: Grafana Loki

------------------------------------------------------------------------

Documento gerado automaticamente em 2026-02-13T01:49:20.474093Z
