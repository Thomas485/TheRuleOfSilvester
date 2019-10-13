﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TheRuleOfSilvester.Network
{
    public enum CommandName : short
    {
        Error = -1, 

        GetMap,
        GetMovableTiles,
        NewPlayer,
        GetPlayers,
        UpdatePlayer,
        TransmitActions,
        EndRound,
        Wait,
        GetStatus,
        GetWinners,
        RegisterPlayer,
        GetSessions,
        JoinSession,
        NewGame,
    }
}
