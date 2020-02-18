def send(message):
    connection = pika.BlockingConnection(
    pika.ConnectionParameters(host="localhost"))
    channel = connection.channel()

    channel.queue_declare(queue="test")

    channel.basic_publish(exchange="", routing_key="test", body=str(message))
    print(" [x] Sent message")

    connection.close()

if __name__ == "__main__":
    send("SPY")
