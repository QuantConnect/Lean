#!/usr/bin/env python
import pika
import json

def recieve():
    connection = pika.BlockingConnection(
    pika.ConnectionParameters(host='localhost'))
    channel = connection.channel()

    channel.queue_declare(queue='hello')


    def callback(ch, method, properties, body):
        message = body.decode('utf8').replace("'", '"')
        message = json.loads(message)

        for equity in message["equities"]:
            print(equity["name"])
            print(equity["orderValue"])
            print(equity["orderType"])


    channel.basic_consume(queue='hello', on_message_callback=callback, auto_ack=True)

    print('RECIEVER: [*] Waiting for messages. To exit press CTRL+C')
    channel.start_consuming()


if __name__=="__main__":
    recieve()
