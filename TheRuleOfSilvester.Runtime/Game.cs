﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheRuleOfSilvester.Core;
using TheRuleOfSilvester.Core.Extensions;
using TheRuleOfSilvester.Core.Observation;
using TheRuleOfSilvester.Network;
using TheRuleOfSilvester.Network.Info;
using TheRuleOfSilvester.Runtime.Cells;
using TheRuleOfSilvester.Runtime.Interfaces;
using TheRuleOfSilvester.Runtime.Roles;

namespace TheRuleOfSilvester.Runtime
{
    public class Game : IDisposable, IGameStatus
    {
        public IObservable<PlayerAction> CurrentUpdateSets { get; internal set; }
        public IDrawComponent DrawComponent { get; set; }
        public IInputComponent InputCompoment { get; set; }
        public IMultiplayerComponent MultiplayerComponent { get; set; }
        public IRoundManagerComponent RoundComponent { get; set; }
        public IWaitingComponent WaitingComponent { get; set; }
        public GameStatus CurrentGameStatus { get; set; }

        public int Frames { get; private set; }

        public Map Map
        {
            get => map; private set
            {
                map = value;
                if (RoundComponent == null)
                    RoundComponent = new DefaultRoundManagerComponent(map);
            }
        }

        public bool IsMutliplayer { get; private set; }

        internal InputAction InputAction { get; private set; }
        public IObservable<Player> Winners { get; private set; }

        private readonly ManualResetEventSlim manualResetEvent;
        private CancellationTokenSource tokenSource;
        private Task gameTask;
        private Player player;
        private Map map;
        private readonly Subject<(CommandName, Notification)> commandSubject;

        public Game()
        {
            CurrentUpdateSets = Observable.Empty<PlayerAction>();
            manualResetEvent = new ManualResetEventSlim(false);
            commandSubject = new Subject<(CommandName, Notification)>();
        }

        public void RunMultiplayer(int frame, int sessionId)
        {
            IsMutliplayer = true;
            MultiplayerComponent.Connect();

            MultiplayerComponent
                .GetNotifications()
                .Where(n => n.Type == NotificationType.Map)
                .Select(n => n.Deserialize(SerializeHelper.Deserialize<Map>))
                .Subscribe(m =>
                {
                    Map = m;
                    m.SubscribePlayerNotifications(MultiplayerComponent.GetNotifications());
                });

            MultiplayerComponent
                  .CurrentServerStatus
                  .Where(s => s == ServerStatus.Started)
                  .Subscribe(s =>
                  {
                      CurrentGameStatus = GameStatus.Running;
                  });

            Winners = MultiplayerComponent
                .GetNotifications()
                .Where(n => n.Type == NotificationType.Winner)
                .Select(n => n.Deserialize(SerializeHelper.Deserialize<Player>));

            MultiplayerComponent.SendPackages(commandSubject);
            commandSubject.OnNext((CommandName.JoinSession, new Notification(sessionId.GetBytes(), NotificationType.None)));
            Run(frame);
        }

        public void RunSinglePlayer(int frame, int x, int y)
        {
            var generator = new MapGenerator();
            Map = generator.Generate(x, y);

            player = new Player(Map, RoleManager.GetRandomRole())
            {
                IsLocal = true,
                Position = new Position(7, 4)
            };
            CurrentGameStatus = GameStatus.Running;

            Map.AddPlayer(player);
            Run(frame);
        }

        public void Stop()
        {
            if (IsMutliplayer)
                MultiplayerComponent.Disconnect();

            tokenSource?.Cancel();
            tokenSource?.Dispose();
            tokenSource = null;

            gameTask?.Dispose();
            gameTask = null;

            CurrentGameStatus = GameStatus.Stopped;
        }

        public void Update()
        {
            SystemUpdate();
            UiUpdate();
            AfterUpdate();
        }

        public void Wait()
            => manualResetEvent.Wait();

        public void Dispose()
        {
            tokenSource?.Cancel();

            tokenSource?.Dispose();
            gameTask?.Dispose();
            player?.Dispose();
            manualResetEvent?.Dispose();

            tokenSource = null;
            gameTask = null;
            player = null;
        }

        private void Run(int frame)
        {
            Frames = frame;

            WaitingComponent?.SubscribeGameStatus(this);
            tokenSource = new CancellationTokenSource();

            InputCompoment.InputActions?.Subscribe(a =>
            {
                InputAction?.SetInvalid();
                InputAction = a;
            });

            gameTask = new Task(async () =>
            {
                try
                {
                    await Loop(tokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }, TaskCreationOptions.LongRunning);

            gameTask.Start(TaskScheduler.Default);
        }

        private void SystemUpdate()
        {
            if (InputAction?.Type == InputActionType.Back)
                Stop();

            if (CurrentGameStatus == GameStatus.Running)
                GameUpdate();
        }

        private void GameUpdate()
        {
            player?.Update(this);
            RoundComponent.Update(this);
        }

        private void UiUpdate()
        {
            switch (CurrentGameStatus)
            {
                case GameStatus.Running:
                    DrawComponent.Draw(Map);
                    break;
                case GameStatus.Paused:
                case GameStatus.Waiting:
                    DrawComponent.DrawTextCells(new List<TextCell> { new TextCell("NOT Running", Map) });
                    break;
                case GameStatus.Stopped:
                default:
                    return;
            }
        }

        private void AfterUpdate()
        {
            InputAction?.SetInvalid();
            InputAction = null;
        }

        private async Task Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Update();
                await Task.Delay(1000 / Frames, token);
            }

            manualResetEvent.Set();
        }

    }
}
