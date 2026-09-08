#!/usr/bin/env bash
# Declara, de forma idempotente, as exchanges/filas/bindings que a Azure Function de
# Notifications precisa encontrar prontas no CloudAMQP antes do primeiro deploy.
#
# Antes, quem criava essa topologia era o próprio FCG.Notifications.Worker ao subir (MassTransit
# registra consumer -> declara fila e binding automaticamente). Sem esse Worker rodando 24/7,
# a topologia precisa ser declarada uma única vez, manualmente, via API de management do RabbitMQ.
#
# Uso:
#   RABBITMQ_MGMT_URL="https://SEU-HOST.rmq.cloudamqp.com" \
#   RABBITMQ_VHOST="seu-vhost" \
#   RABBITMQ_USER="usuario" \
#   RABBITMQ_PASSWORD="senha" \
#   ./scripts/setup-topology.sh

set -euo pipefail

: "${RABBITMQ_MGMT_URL:?defina RABBITMQ_MGMT_URL, ex: https://seu-host.rmq.cloudamqp.com}"
: "${RABBITMQ_VHOST:?defina RABBITMQ_VHOST}"
: "${RABBITMQ_USER:?defina RABBITMQ_USER}"
: "${RABBITMQ_PASSWORD:?defina RABBITMQ_PASSWORD}"

VHOST_ENC=$(python3 -c "import urllib.parse,sys; print(urllib.parse.quote(sys.argv[1], safe=''))" "$RABBITMQ_VHOST")
AUTH=(-u "${RABBITMQ_USER}:${RABBITMQ_PASSWORD}")

declare_exchange() {
  local name="$1"
  curl -sf "${AUTH[@]}" -X PUT "${RABBITMQ_MGMT_URL}/api/exchanges/${VHOST_ENC}/${name}" \
    -H "content-type: application/json" \
    -d '{"type":"fanout","durable":true}' \
    && echo "exchange ${name}: ok"
}

# Fila "quorum" — tipo replicado/durável recomendado para filas RabbitMQ acessadas por
# consumidores externos (Azure Function via RabbitMQTrigger).
declare_queue() {
  local name="$1"
  curl -sf "${AUTH[@]}" -X PUT "${RABBITMQ_MGMT_URL}/api/queues/${VHOST_ENC}/${name}" \
    -H "content-type: application/json" \
    -d '{"durable":true,"arguments":{"x-queue-type":"quorum"}}' \
    && echo "queue ${name}: ok"
}

declare_binding() {
  local exchange="$1" queue="$2"
  curl -sf "${AUTH[@]}" -X POST "${RABBITMQ_MGMT_URL}/api/bindings/${VHOST_ENC}/e/${exchange}/q/${queue}" \
    -H "content-type: application/json" \
    -d '{"routing_key":""}' \
    && echo "binding ${exchange} -> ${queue}: ok"
}

declare_exchange "user-created-event"
declare_exchange "payment-processed-event"

declare_queue "notifications-user-created-event"
declare_queue "notifications-payment-processed-event"

declare_binding "user-created-event" "notifications-user-created-event"
declare_binding "payment-processed-event" "notifications-payment-processed-event"

echo "Topologia pronta."
