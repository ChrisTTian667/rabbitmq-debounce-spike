FROM rabbitmq:3.11-management

# Install necessary packages
RUN apt-get update && \
    apt-get install -y curl git build-essential libssl-dev

# Install rabbitmq_delayed_message_exchange plugin
RUN curl -L https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/3.11.1/rabbitmq_delayed_message_exchange-3.11.1.ez -o $RABBITMQ_HOME/plugins/rabbitmq_delayed_message_exchange-3.11.1.ez && \
    chown rabbitmq:rabbitmq $RABBITMQ_HOME/plugins/rabbitmq_delayed_message_exchange-3.11.1.ez

# Install rabbitmq_message_deduplication plugin
RUN curl -L https://github.com/noxdafox/rabbitmq-message-deduplication/releases/download/0.6.2/elixir-1.14.0.ez -o $RABBITMQ_HOME/plugins/elixir-1.14.0.ez && \
    chown rabbitmq:rabbitmq $RABBITMQ_HOME/plugins/elixir-1.14.0.ez && \
    curl -L https://github.com/noxdafox/rabbitmq-message-deduplication/releases/download/0.6.2/rabbitmq_message_deduplication-0.6.2.ez -o $RABBITMQ_HOME/plugins/rabbitmq_message_deduplication-0.6.2.ez && \
    chown rabbitmq:rabbitmq $RABBITMQ_HOME/plugins/rabbitmq_message_deduplication-0.6.2.ez

# Enable the plugins
RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange \
    rabbitmq_message_deduplication \
    rabbitmq_shovel \
    rabbitmq_shovel_management

# Clean up
RUN apt-get remove --purge -y git build-essential libssl-dev && \
    apt-get autoremove -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

EXPOSE 5672 15672

# Copy configuration files
COPY rabbitmq.conf /etc/rabbitmq/rabbitmq.conf
