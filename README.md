# Debounce

This project demonstrates the usage of RabbitMQ to process jobs using debouncing. When multiple messages with the same payload are invoked within a specified delay period, only the last message is processed.

## Features

- **Queue Jobs**: Queue jobs by sending a POST request to the API's endpoint `/queue-job` with the message payload and a delay in milliseconds.
- **Debouncing**: Ensures that only the last message with the same payload is processed within the delay period.
- **RabbitMQ Plugins**: Utilizes the `rabbitmq_delayed_message_exchange`, `rabbitmq_message_deduplication`, and `rabbitmq_shovel` plugins.

## Invoke a Message

```sh
curl --request POST \
--url 'http://localhost:5000/queue-job?message=HelloWorld\&delay=3000'
```

## Getting Started

### Prerequisites

1. [Docker](https://www.docker.com/products/docker-desktop)
2. [Docker Compose](https://docs.docker.com/compose/install/)
3. [.NET SDK](https://dotnet.microsoft.com/download)

### Installation

1. **Start RabbitMQ**:
   ```sh
   docker-compose up
   ```

2. **Run the Project**:
   ```sh
   dotnet run --project src/Debounce.Api/Debounce.Api.csproj
   ```

3. **Invoke a Message**:
   Use the curl command provided above to test the API.

## RabbitMQ Plugins

This project uses the following RabbitMQ plugins to achieve the desired functionality:

1. **[rabbitmq_delayed_message_exchange](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange)**
2. **[rabbitmq_message_deduplication](https://github.com/noxdafox/rabbitmq-message-deduplication)**
3. **[rabbitmq_shovel](https://www.rabbitmq.com/shovel.html)**

These plugins are automatically installed and configured by the Docker setup provided in this project.

## Build Instructions

Build the project using [Cake](https://cakebuild.net).

1. **Restore Local Tools**:
   ```sh
   dotnet tool restore
   ```

2. **Run Cake**:
   ```sh
   dotnet cake
   ```

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any changes or improvements.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
