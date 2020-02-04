#!/usr/bin/env python
import json
from send import send
from recieve import recieve

def main():
    with open("message.json", "r") as message:
        message = json.loads(message.read())
        send(message)

    recieve()

if __name__=="__main__":
    main()
