﻿using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using TheRuleOfSilvester.Network;
using TheRuleOfSilvester.Network.Sessions;
using TheRuleOfSilvester.Server.Commands;

namespace TheRuleOfSilvester.Server
{
    public sealed class GameServerSession : ServerSession, IGameServerSession
    {
        public int MaxPlayers => 4;
        public string Name => "";
        public int CurrentPlayers => gameManager.Players.Count;

        private readonly GameManager gameManager;
        private readonly PlayerService playerService;

        public GameServerSession(PlayerService playerService) : base()
        {
            gameManager = new GameManager();
            this.playerService = playerService;
        }

        protected override IDisposable RegisterCommands(IObservable<CommandNotification> commands, 
            out IObservable<CommandNotification> notifications)
        {
            var disposables = new CompositeDisposable
            {
                RegisterCommand<GeneralCommandObserver>(commands, out var generalNotifications, gameManager, playerService),
                RegisterCommand<MapCommandObserver>(commands, out var mapNotifications, gameManager, playerService),
                RegisterCommand<RoundCommandObserver>(commands, out var roundNotifications, gameManager, playerService)
            };

            notifications = Observable.Merge(generalNotifications, mapNotifications, roundNotifications);

            return disposables;
        }
    }
}