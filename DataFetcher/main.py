#!/usr/bin/env python
import pika
import json
import zipfile
import quandl
import os
import pandas
from zipfile import ZipFile

API_KEY = "PgJuoJUUrmZVu75mRUD2"


def main():
	# Opens example message for RabbitMQMessage
	with open("message.json", "r") as message:
		message = json.loads(message.read())
		send(message)

	recieve()


def callback(ch, method, properties, body):
	try:
		equityCall = body.decode("utf8").replace("\'", "\"")
		equityCall = json.loads(equityCall)

		# Gets each equity for each timeframe
		for timeFrame in equityCall["timeFrames"]:
			for ticker in timeFrame["equities"]:
				writeData(timeFrame, ticker)

	except:
		print("RECIEVE: Incorrect RabbitMQ message format")


def writeData(equityCall, ticker):
	# If path for equity does not exist create one
	outname = ticker.lower() + ".csv"
	zipname = ticker.lower() + ".zip"

	outdir = "../Data/equity/usa/"+equityCall["resolution"]+"/"
	if not os.path.exists(outdir):
	    os.makedirs(outdir)

	# Full path to equity csvzip
	fullname = os.path.join(outdir, outname)
	zipname = os.path.join(outdir, zipname)

	# Makes Quandl API call for ticker as provided by RabbitMQMessage
	df = quandl.get(
		"WIKI/"+ticker,
		start_date=equityCall["startTime"],
		end_date=equityCall["endTime"],
		api_key=API_KEY)

	# Multiply values by 10000 to fit Lean format
	for header in df.columns[0:4].tolist():
		df[header] = df[header].apply(lambda x: int(x * 10000))

	df["Volume"] = df["Volume"].apply(lambda x: int(x))
	df.index = pandas.to_datetime(df.index,
		format = '%m/%d/%Y').strftime('%Y%m%d 00:00')

	# Drop unused columns from dataframe
	df = df.drop(["Ex-Dividend",
			"Split Ratio",
			"Adj. Open",
			"Adj. High",
			"Adj. Low",
			"Adj. Close",
			"Adj. Volume"],
			axis=1)

	# print(df)

	# Write csvzip to path
	df.to_csv(fullname, header=False)
	ZipFile(zipname, mode="w").write(fullname, os.path.basename(fullname))
	os.remove(fullname)


def recieve():
    connection = pika.BlockingConnection(
    pika.ConnectionParameters(host="localhost"))
    channel = connection.channel()

    channel.queue_declare(queue="equities")

    channel.basic_consume(queue="equities", on_message_callback=callback, auto_ack=True)

    print("RECIEVER: [*] Waiting for messages. To exit press CTRL+C")
    channel.start_consuming()


def send(message):
    connection = pika.BlockingConnection(
    pika.ConnectionParameters(host="localhost"))
    channel = connection.channel()

    channel.queue_declare(queue="stock")

    channel.basic_publish(exchange="", routing_key="equities", body=str(message))
    print(" [x] Sent message")

    connection.close()

if __name__ == "__main__":
    main()
