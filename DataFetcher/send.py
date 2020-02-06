#!/usr/bin/env python
import pika
import json

def send(message):
    connection = pika.BlockingConnection(
    pika.ConnectionParameters(host='localhost'))
    channel = connection.channel()

    channel.queue_declare(queue='stock')

    channel.basic_publish(exchange='', routing_key='hello', body=str(message))
    print(" [x] Sent message")

    connection.close()


if __name__=="__main__":
    with open("message.json", "r") as message:
        message = json.loads(message.read())
        send(message)
