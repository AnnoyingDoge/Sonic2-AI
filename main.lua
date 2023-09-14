console.log('Init')

--read and store two main bytes for position (x-coordinate of camera)
position = mainmemory.read_u16_be(0xEE00)
--"score" of AI-player, used for feedback
fitness = 0

--moves player right
function moveRight ()
    joypad.set({Right=true, B=true}, 1)
end

function updateFitness ()
    fitness = position
end

--main script loop
while true do
    position = mainmemory.read_u16_be(0xEE00)

    if position > fitness then
        updateFitness()
    end

    gui.text(50, 150, 'Position ' .. tostring(position))
    gui.text(50, 200, 'Fitness ' .. tostring(fitness))

    emu.frameadvance()
end

