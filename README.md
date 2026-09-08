# FCG.Notifications — Microsserviço de Notificações (Serverless)

Este repositório contém o **microsserviço de Notificações** do sistema FIAP Cloud Games (FCG).
Ele é uma das quatro peças independentes que formam o sistema — para entender como ele se
conecta com as outras, veja o repositório
[`FCG.Orchestration`](../FCG.Orchestration),
que explica o projeto como um todo em linguagem simples.

Este README explica só a parte deste serviço, também em linguagem simples, assumindo que quem
lê não tem experiência técnica.

---

## 1. O que este serviço faz

Este serviço é responsável por **simular o envio de e-mails** em dois momentos da jornada do
usuário:

1. **Boas-vindas** — quando um novo usuário se cadastra na plataforma.
2. **Confirmação de compra** — quando o pagamento de uma compra de jogo é aprovado.

Ele não manda e-mails de verdade (não há integração com um serviço de e-mail real) — ele
**simula** o envio escrevendo uma mensagem no log do sistema (Application Insights). Isso é o
suficiente para demonstrar o fluxo do sistema sem depender de infraestrutura externa de e-mail.

### Arquitetura: função serverless (Azure Functions), não mais container 24/7

Diferente da Fase 2, este serviço **não roda mais continuamente** como container/Worker Service.
Ele agora é uma **Azure Function** (`FCG.Notifications.Function`), que fica **desligada** o
tempo todo e só é executada quando o RabbitMQ tem uma mensagem nova para entregar. O próprio
Azure Functions tem um **trigger nativo de RabbitMQ** (`RabbitMQTrigger`) que observa a fila e
aciona a função diretamente — não existe processo nosso rodando 24/7 esperando mensagens, e não
há polling: a função é chamada no exato momento em que a mensagem chega.

Isso atende ao requisito da Fase 3 (Item 2 — Migração para arquitetura Serverless): "função
executada automaticamente disparada por novas mensagens numa fila/tópico do sistema de
mensageria", sem precisar reescrever o fluxo de negócio.

### Os dois fluxos que ele participa

**Fluxo 1 — Boas-vindas:**

```
UsersAPI publica UserCreatedEvent  ──►  Function é acionada  ──►  "envia" (loga) e-mail de boas-vindas
```

**Fluxo 2 — Confirmação de compra:**

```
PaymentsAPI publica PaymentProcessedEvent  ──►  Function é acionada
                                                       │
                                          Status == Approved?
                                          ├─ sim → "envia" (loga) e-mail de confirmação
                                          └─ não → não faz nada
```

---

## 2. Como as peças se conectam (RabbitMQ + CloudAMQP)

Este serviço não fala diretamente com nenhum outro. Ele troca mensagens através do
**RabbitMQ**, um "correio" compartilhado entre todos os microsserviços (veja a explicação
completa no README do repositório de orquestração).

Como a Azure Function precisa alcançar o RabbitMQ pela internet para o trigger funcionar
(o broker local do `docker-compose`, rodando no computador de cada dev, não tem endereço
público), o broker compartilhado foi migrado para o **CloudAMQP** (plano gratuito "Little
Lemur") — um RabbitMQ hospedado, com endereço público e TLS. Ver seção 4 do README do
`FCG.Orchestration` para os detalhes dessa migração.

| Direção | Evento | Exchange no RabbitMQ | Fila (queue) |
|---|---|---|---|
| Recebe (consome) | `UserCreatedEvent` | `user-created-event` | `notifications-user-created-event` |
| Recebe (consome) | `PaymentProcessedEvent` | `payment-processed-event` | `notifications-payment-processed-event` |

O nome de cada fila é prefixado com o nome do serviço (`notifications-`) para que dois serviços
independentes que consomem o mesmo tipo de evento não acabem compartilhando uma única fila
física — é o caso do `PaymentProcessedEvent`, que tanto este serviço quanto o Catálogo
consomem. Sem o prefixo, os dois disputariam a mesma fila e as mensagens seriam divididas entre
eles, em vez de cada serviço receber todas.

> **Importante:** como não existe mais um Worker rodando 24/7 para declarar essa topologia
> automaticamente (era o MassTransit quem fazia isso, ao subir, na Fase 2), as exchanges/filas/
> bindings precisam ser criadas manualmente uma única vez, antes do primeiro deploy. Ver
> `scripts/setup-topology.sh` (seção 5).

---

## 3. Estrutura de pastas

```
FCG.Notifications/
├── FCG.Notifications.Domain/       # regras de negócio (não sabe nada sobre RabbitMQ/Azure)
│   ├── Dto/                         # formato dos eventos (UserCreatedEvent, PaymentProcessedEvent)
│   ├── Enums/                       # PaymentStatus (Approved, Rejected)
│   ├── Interfaces/IService/         # contratos dos serviços
│   ├── Services/                    # NotificationService — monta o texto dos e-mails simulados
│   └── Validators/                  # validação dos eventos recebidos (FluentValidation)
├── FCG.Notifications.Function/     # a Azure Function (ponto de entrada serverless)
│   ├── Function.cs                  # dois triggers RabbitMQ: HandleUserCreated, HandlePaymentProcessed
│   ├── Program.cs                   # bootstrap do isolated worker
│   ├── host.json                    # configuração de runtime/telemetria
│   └── FCG.Notifications.Function.csproj
├── FCG.Notifications.Tests/         # testes automatizados
├── scripts/setup-topology.sh        # cria exchanges/filas/bindings no CloudAMQP (rodar 1x)
└── infra/                           # Infraestrutura como código (Azure Bicep)
```

---

## 4. Deploy (Azure Functions)

Este serviço é implantado no **Azure Functions** (Consumption plan), com o RabbitMQ trigger
apontando para a instância CloudAMQP. A infraestrutura (resource group, storage account, Function
App, Application Insights) é definida em `infra/main.bicep`.

### Pré-requisitos

- Conta Azure (assinatura pay-as-you-go; a cota "always free" do Consumption plan cobre o uso
  deste projeto sem custo).
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) e
  [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
  instalados e autenticados (`az login`).
- Instância CloudAMQP criada (endereço, vhost, usuário, senha).

### Passo 1 — criar a topologia no CloudAMQP (uma única vez)

```bash
RABBITMQ_MGMT_URL="https://SEU-HOST.rmq.cloudamqp.com" \
RABBITMQ_VHOST="seu-vhost" \
RABBITMQ_USER="usuario" \
RABBITMQ_PASSWORD="senha" \
./scripts/setup-topology.sh
```

### Passo 2 — provisionar a infraestrutura (Bicep)

```bash
az deployment group create \
  --resource-group <nome-do-resource-group> \
  --template-file infra/main.bicep \
  --parameters functionAppName=<nome-da-function-app>
```

### Passo 3 — configurar a connection string do RabbitMQ

```bash
az functionapp config appsettings set \
  --name <nome-da-function-app> \
  --resource-group <nome-do-resource-group> \
  --settings "RabbitMqConnection=amqps://usuario:senha@seu-host.rmq.cloudamqp.com/vhost"
```

### Passo 4 — build e deploy do código

```bash
func azure functionapp publish <nome-da-function-app> --dotnet-isolated
```

### Passo 5 — ver os logs

Como não há mais container/terminal rodando, os logs (e-mails "enviados", erros de validação)
vão para o **Application Insights** vinculado à Function App:

```bash
az monitor app-insights query \
  --app <nome-da-function-app> \
  -g <nome-do-resource-group> \
  --analytics-query "traces | order by timestamp desc | take 20"
```

---

## 5. Testes

```bash
dotnet test FCG.Notifications.Tests
```

## 6. Limitações conhecidas

- Os e-mails são **simulados via log**, não há integração com um serviço de e-mail real.
- Eventos que falham na validação são descartados com um log de aviso — não há retry automático
  nem fila de mensagens mortas (*dead-letter queue*) configurada.
- A topologia do RabbitMQ (exchanges/filas/bindings) precisa ser criada manualmente uma vez via
  `scripts/setup-topology.sh` antes do primeiro deploy — não há mais um Worker que a declare
  automaticamente ao subir.
