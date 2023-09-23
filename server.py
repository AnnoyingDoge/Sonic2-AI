#webserver will receieve fitness data, position data, level data, etc. then we can write training stuff in python
#requires websockets (pip install websockets)
#websockets documentation was very useful!!!

import asyncio
import websockets

def unpackage(packet: bytes):
    isInt = False
    isString = False
    values = []
    
    i = 0
    while(i < len(packet)):
        if isInt:
            intBytes = packet[i], packet[i+1], packet[i+2], packet[i+3]
            values.append(int.from_bytes(intBytes, "big"))
            isInt = False
            i+=4
        if isString:
            endIndex = -1
            #search for end index
            for j in range(i, len(packet)):
                print(j)
                #end of string marked by 0
                if packet[j] == 0:
                    endIndex = j
                #once found, break loop
                if endIndex != -1:
                    break
            stringBytes = packet[i:j]
            values.append(str(stringBytes, 'UTF-8'))
            i+=endIndex-i
        
        #int identification bytes == (255,255,255,255)
        if packet[i] == 255 and (packet[i] == packet[i+1] == packet[i+2] == packet[i+3]):
            isInt = True
            i+=4

        #string identification bytes == (255,255,255,0)
        elif packet[i] == 255 and (packet[i] == packet[i+1] == packet[i+2]) and packet[i+3] == 0:
            isString = True
            i+=4


    # for i in range(0, len(packet), 4):
    #     #if we have int upcoming, read as int and store in list
    #     if isInt:
    #         intBytes = packet[i], packet[i+1], packet[i+2], packet[i+3]
    #         values.append(int.from_bytes(intBytes, "big"))
    #         isInt = False

    #     if isString:
    #         endIndex = -1
    #         #search for end index
    #         for j in range(i, len(packet)):
    #             print(j)
    #             #end of string marked by 0
    #             if packet[j] == 0:
    #                 endIndex = j
    #             #once found, break loop
    #             if endIndex != -1:
    #                 break
    #         stringBytes = packet[i:j]
    #         values.append(str(stringBytes, 'UTF-8'))

    #     #int identification bytes == (255,255,255,255)
    #     if packet[i] == 255 and (packet[i] == packet[i+1] == packet[i+2] == packet[i+3]):
    #         isInt = True

    #     #string identification bytes == (255,255,255,0)
    #     elif packet[i] == 255 and (packet[i] == packet[i+1] == packet[i+2]) and packet[i+3] == 0:
    #         isString = True

    return values


#coroutine that manages a connection
async def handler(websocket):
    message = "msg"
    last_message = ""
    while True:
        #receive and print a message forever!
        message = await websocket.recv()
        if last_message != message:
            print(unpackage(message))
            last_message = message


async def main():
    #call serve() to create webserver (coroutine to call for a connection, interfaces where server can be reached, port)
    async with websockets.serve(handler, "", 8001):
        await asyncio.Future()  # run forever


if __name__ == "__main__":
    asyncio.run(main())