ent-HermoDverTypeOne = гермодверь
    .desc = Защитно-герметические и герметические устройства, которые используются для охраны защитных сооружений гражданской обороны от вредных воздействий различного характера.
    .suffix = ST, mapping
ent-HermoDverTypeTwo = { ent-HermoDverTypeOne }
    .desc = { ent-HermoDverTypeOne.desc }
    .suffix = { ent-HermoDverTypeOne.suffix }
ent-LatticeDoor = решётчатая дверь
    .desc = Это один из видов конструкций для установки в разные виды помещений с целью повышения безопасности.
    .suffix = { ent-HermoDverTypeOne.suffix }
ent-StalkerGate = Створка ворот
    .desc = Жёсткая рама, которую изготавливают из трубы квадратного сечения. Облицовкой служит металлический профнастил, покрытый полимерным покрытием. Неплохо сохранилась для своих лет.
    .suffix = ST, mapping, левая
ent-StalkerGater = { ent-StalkerGate }
    .desc = { ent-StalkerGate.desc }
    .suffix = ST, mapping, правая
ent-StalkerGateFixed = { ent-StalkerGate }
    .desc = { ent-StalkerGate.desc }
    .suffix = ST, mapping, левая, незакрываемая
ent-StalkerGaterFixed = { ent-StalkerGate }
    .desc = { ent-StalkerGate.desc }
    .suffix = ST, mapping, правая, незакрываемая
ent-MilitaryDoor2 = военные ворота
    .desc = Проезд в стене или ограде, запираемый створами.
    .suffix = { ent-HermoDverTypeOne.suffix }