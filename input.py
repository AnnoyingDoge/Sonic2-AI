#control game input

def dpad(direction: str):
    if direction == 'left':
        return 1
    elif direction == 'right':
        return 2
    elif direction == 'up':
        return 3
    elif direction == 'down':
        return 4
    else:
        return 0
    
