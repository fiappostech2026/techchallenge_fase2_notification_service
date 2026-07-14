# FCG.Notifications — Microsserviço de Notificações

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
**simula** o envio escrevendo uma mensagem no console/log do sistema. Isso é o suficiente para
demonstrar o fluxo do sistema sem depender de infraestrutura externa de e-mail.

Assim como o serviço de Pagamentos, ele não tem tela nem endereço que se acessa pelo navegador.
Ele fica em segundo plano, esperando avisos (**eventos**) chegarem pelo RabbitMQ. Esse tipo de
programa é chamado de **Worker Service**.

### Os dois fluxos que ele participa

**Fluxo 1 — Boas-vindas:**

```
UsersAPI publica UserCreatedEvent  ──►  este serviço recebe  ──►  "envia" (loga) e-mail de boas-vindas
```

**Fluxo 2 — Confirmação de compra:**

```
PaymentsAPI publica PaymentProcessedEvent  ──►  este serviço recebe
                                                       │
                                          Status == Approved?
                                          ├─ sim → "envia" (loga) e-mail de confirmação
                                          └─ não → não faz nada
```

---

## 2. Como as peças se conectam (RabbitMQ)

Este serviço não fala diretamente com nenhum outro. Ele troca mensagens através do
**RabbitMQ**, um "correio" compartilhado entre todos os microsserviços (veja a explicação
completa no README do repositório de orquestração).

| Direção | Evento | Exchange no RabbitMQ | Fila (queue) |
|---|---|---|---|
| Recebe (consome) | `UserCreatedEvent` | `user-created-event` | `notifications-user-created-event` |
| Recebe (consome) | `PaymentProcessedEvent` | `payment-processed-event` | `notifications-payment-processed-event` |

O nome de cada fila é prefixado com o nome do serviço (`notifications-`, via
`SetEndpointNameFormatter`) para que dois serviços independentes que consomem o mesmo tipo de
evento não acabem compartilhando uma única fila física — é o caso do `PaymentProcessedEvent`, que
tanto este serviço quanto o Catálogo consomem. Sem o prefixo, os dois disputariam a mesma fila e
as mensagens seriam divididas entre eles, em vez de cada serviço receber todas.

A biblioteca usada para falar com o RabbitMQ é o **MassTransit**, um "tradutor" que evita ter
que lidar com os detalhes técnicos do protocolo do RabbitMQ na mão. A serialização das mensagens
usa `UseRawJsonSerializer(RawSerializerOptions.All, isDefault: true)` em vez do envelope padrão do
MassTransit, porque cada serviço define sua própria cópia local de cada evento (em seu próprio
namespace), então a correspondência é feita pela estrutura do JSON, não pelo nome técnico da classe.

---

## 3. Estrutura de pastas

```
FCG.Notifications/
├── FCG.Notifications.Domain/     # regras de negócio (não sabe nada sobre RabbitMQ/Web)
│   ├── Dto/                       # formato dos eventos (UserCreatedEvent, PaymentProcessedEvent)
│   ├── Enums/                     # PaymentStatus (Approved, Rejected)
│   ├── Interfaces/IService/       # contratos dos serviços
│   ├── Services/                  # NotificationService — monta o texto dos e-mails simulados
│   └── Validators/                # validação dos eventos recebidos (FluentValidation)
├── FCG.Notifications.Worker/     # ponto de entrada do programa (roda em background)
│   ├── Consumers/                 # "ouvintes" dos eventos do RabbitMQ
│   ├── Extensions/                # configuração (RabbitMQ, logs, injeção de dependência)
│   ├── Middleware/                # tratamento de erros
│   └── Program.cs                 # arquivo que liga tudo e inicia o serviço
├── FCG.Notifications.Tests/       # testes automatizados
├── k8s/                            # manifestos de deploy no Kubernetes (ver seção 6)
└── Dockerfile                      # receita para empacotar este serviço em um container
```

---

## 4. Como rodar sozinho (sem os outros microsserviços)

Você pode rodar este serviço isolado, desde que tenha um RabbitMQ disponível — útil para testar
sem precisar subir o projeto inteiro.

### Opção A — via .NET direto (para quem tem o SDK instalado)

```bash
dotnet run --project FCG.Notifications.Worker
```

### Opção B — via Docker

```bash
docker build -t fcg-notifications-api:latest .
docker run --rm -e RABBITMQ__HOST=host.docker.internal fcg-notifications-api:latest
```

> Para rodar o sistema completo (com RabbitMQ e o serviço de Pagamentos juntos, prontos para os
> fluxos funcionarem de ponta a ponta), use o `docker-compose.yml` do repositório
> [`FCG.Orchestration`](../FCG.Orchestration) —
> é o caminho recomendado.

---

## 5. Variáveis de ambiente

| Variável | Descrição | Valor padrão (local) |
|---|---|---|
| `RabbitMQ__Host` | Endereço do servidor RabbitMQ | `localhost` |
| `RabbitMQ__VirtualHost` | "Vhost" (espaço isolado) do RabbitMQ a usar | `/` |
| `RabbitMQ__Username` | Usuário de acesso ao RabbitMQ | `guest` |
| `RabbitMQ__Password` | Senha de acesso ao RabbitMQ | `guest` |

> No Docker/Kubernetes, essas variáveis são escritas com **duplo underscore**
> (`RABBITMQ__HOST`), que é a forma como o .NET entende "seção : chave" vindo de variáveis de
> ambiente do sistema operacional.

---

## 6. Deploy no Kubernetes

Os manifestos estão na pasta `/k8s` deste repositório:

| Arquivo | O que faz |
|---|---|
| `deployment.yaml` | Sobe o container deste serviço, reiniciando sozinho se cair |
| `service.yaml` | Dá o nome de rede `notifications-api` para outros serviços acharem este |
| `configmap.yaml` | Guarda o endereço e vhost do RabbitMQ (não são segredos) |
| `secret.yaml` | Guarda usuário/senha do RabbitMQ (dado sensível) |

Para aplicar:

```bash
kubectl apply -f k8s/
```

> **Pré-requisito:** o RabbitMQ precisa já estar rodando no cluster com o nome de Service
> `rabbitmq` — ele é definido no repositório
> [`FCG.Orchestration`](../FCG.Orchestration).

---

## 7. Testes

```bash
dotnet test FCG.Notifications.Tests
```

## 8. Limitações conhecidas

- Os e-mails são **simulados via log**, não há integração com um serviço de e-mail real.
- Este serviço ainda não expõe um endpoint de healthcheck HTTP (diferente do PaymentsAPI, que
  expõe `/healthz`) — não há `AddHealthChecks()`/`MapHealthChecks()` configurado no `Program.cs`
  atual.
- Eventos que falham na validação são descartados com um log de aviso — não há retry automático
  nem fila de mensagens mortas (*dead-letter queue*) configurada.
