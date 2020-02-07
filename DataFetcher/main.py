#!/usr/bin/env python
import pika
import json
import pandas as pd
import yfinance as yf
import zipfile
import time
from recieve import recieve
from send import send


def main():
	with open("message.json", "r") as message:
	    message = json.loads(message.read())
	    send(message)

	recieve()

def callback(ch, method, properties, body):
	try:
		equityCall = body.decode('utf8').replace("'", '"')
		equityCall = json.loads(equityCall)

		tickers = yf.download(
			equityCall["equities"],
			start=equityCall["startTime"],
			end=equityCall["endTime"],
			period=equityCall["period"],
			auto_adjust = True
		)

		print(tickers)

		# for x in tickers:
		# 	print(x)
	    #     try:
		# 	    data = yf.download(x, period = "1d")
		# 	    z = zipfile.ZipFile('/Algo-Trader-Lean/Data/equity/usa/daily/'+x+'.zip', "a")
		# 	    z.write(data.to_csv(timestr+'.csv'))
		# 	    z.close()
	    #     except:
	    #         continue


	except:
		print("RECIEVE: Incorrect RabbitMQ message format")

def recieve():
    connection = pika.BlockingConnection(
    pika.ConnectionParameters(host='localhost'))
    channel = connection.channel()

    channel.queue_declare(queue='equities')


    channel.basic_consume(queue='equities', on_message_callback=callback, auto_ack=True)

    print('RECIEVER: [*] Waiting for messages. To exit press CTRL+C')
    channel.start_consuming()

def send(message):
    connection = pika.BlockingConnection(
    pika.ConnectionParameters(host='localhost'))
    channel = connection.channel()

    channel.queue_declare(queue='stock')

    channel.basic_publish(exchange='', routing_key='equities', body=str(message))
    print(" [x] Sent message")

    connection.close()

if __name__ == "__main__":
    main()
