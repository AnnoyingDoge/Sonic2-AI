#webserver will receieve fitness data, position data, level data, etc. then we can write training stuff in python
#requires websockets (pip install websockets)
#websockets documentation was very useful!!!

import asyncio
import websockets

#coroutine that manages a connection
async def handler(websocket):
    while True:
        #receive and print a message forever!
        message = await websocket.recv()
        print(message)


async def main():
    #call serve() to create webserver (coroutine to call for a connection, interfaces where server can be reached, port)
    async with websockets.serve(handler, "", 8001):
        await asyncio.Future()  # run forever


if __name__ == "__main__":
    asyncio.run(main())