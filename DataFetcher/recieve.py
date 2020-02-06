#!/usr/bin/env python
import pika
import json

equities = []

def callback(ch, method, properties, body):
	try:
		message = body.decode('utf8').replace("'", '"')
		message = json.loads(message)

		for equity in message["equities"]:
			equityInfo = {
				"name":equity["name"],
				"orderValue":equity["orderValue"],
				"orderType":equity["orderType"]
			}

			equities.append(equityInfo)

			print(equities)
			# print(equity["name"])
			# print(equity["orderValue"])
			# print(equity["orderType"])

	except:
		print("RECIEVE: Incorrect RabbitMQ message format")

def recieve():
    connection = pika.BlockingConnection(
    pika.ConnectionParameters(host='localhost'))
    channel = connection.channel()

    channel.queue_declare(queue='hello')


    channel.basic_consume(queue='hello', on_message_callback=callback, auto_ack=True)

    print('RECIEVER: [*] Waiting for messages. To exit press CTRL+C')
    channel.start_consuming()


if __name__=="__main__":
    recieve()
