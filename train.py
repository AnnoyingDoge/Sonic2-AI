import subprocess
#will look something like this

#setup training

#wait

#run webserver
server = subprocess.run(["python", "server.py"], capture_output=False, text=True)
with open("input.py") as input_script:
    exec(input_script.read())

print(server.stdout)

#train