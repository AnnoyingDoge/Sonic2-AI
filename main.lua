--need to implement websocket
--main lua script to talk to python

console.log('Init')

--read and store two main bytes for position (x-coordinate of camera)
position = mainmemory.read_u16_be(0xEE00)
--"score" of AI-player, used for feedback
fitness = 0

--moves player right
function holdingRight (holding)
    joypad.set({Right=holding}, 1)
end

function holdingLeft ()
    joypad.set({Left=holding})
end

function holdingJump()
    joypad.set({A=holding})
end

--update fitness score
function updateFitness ()
    fitness = position
end

--main script loop
while true do
    position = mainmemory.read_u16_be(0xEE00)

    --only update fitness when we get further than previous
    if position > fitness then
        updateFitness()
    end


    --gui text for reading information
    gui.text(50, 150, 'Position ' .. tostring(position))
    gui.text(50, 200, 'Fitness ' .. tostring(fitness))

    --make emulator go to next frame (rather than waiting for script to end)
    emu.frameadvance()
end

