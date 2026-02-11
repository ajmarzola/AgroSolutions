# RabbitMQ para AgroSolutions

Este diretório contém os manifestos Kubernetes para implantar o RabbitMQ com persistência e interface de gerenciamento.

## Estrutura

- `statefulset.yaml`: Define o cluster RabbitMQ (inicialmente 1 réplica) com persistência de dados.
- `service.yaml`: Serviço ClusterIP para acesso interno (portas 5672 e 15672).
- `headless-service.yaml`: Serviço Headless para descoberta de rede do StatefulSet.
- `secret.yaml`: Credenciais de acesso (usuário e senha).
- `configmap.yaml`: Configurações de conexão para as aplicações consumidoras.

## Como Aplicar

Para implantar o RabbitMQ no cluster, execute o seguinte comando a partir da raiz do repositório (diretório que contém `infra/`):

```bash
kubectl apply -k infra/k8s/rabbitmq
```

Ou navegando até este diretório:

```bash
cd infra/k8s/rabbitmq
kubectl apply -k .
```

## Acessando o Painel de Gerenciamento (Management UI)

O plugin de gerenciamento está habilitado na porta 15672 (interna). No ambiente local, ele é exposto na porta **30006**.

Acesse no navegador: [http://localhost:30006](http://localhost:30006)

## Credenciais

As credenciais padrão estão definidas no arquivo `secret.yaml`:

- **Usuário:** `user`
- **Senha:** `password`

> **Nota:** Em um ambiente de produção real, certifique-se de alterar essas credenciais e gerenciar os Secrets de forma segura (por exemplo, usando SealedSecrets ou Vault).

## Integração com Microsserviços

As definições de conexão estão exportadas via ConfigMap `rabbitmq-config`:

- `RABBITMQ_HOST`: `rabbitmq`
- `RABBITMQ_PORT`: `5672`
- `RABBITMQ_EXCHANGE`: `agrosolutions.exchange`
- `RABBITMQ_ROUTING_KEY`: `default.key`

Os deployments das aplicações (Analise, Ingestao, Propriedades, Usuarios) já foram configurados para carregar essas variáveis de ambiente.
