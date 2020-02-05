#!/usr/bin/env python
import pika
import json

def recieve():
	
    tickers = []
    connection = pika.BlockingConnection(
    pika.ConnectionParameters(host='localhost'))
    channel = connection.channel()

    channel.queue_declare(queue='stock')


    def callback(ch, method, properties, body):
       
	message = body.decode('utf8').replace("'", '"')
        message = json.loads(message)

        for equity in message["equities"]:
            tickers.append(equity["name"])
	



    channel.basic_consume(queue='stock', on_message_callback=callback, auto_ack=True)

    channel.start_consuming()
    return tickers

if __name__=="__main__":
    recieve()
