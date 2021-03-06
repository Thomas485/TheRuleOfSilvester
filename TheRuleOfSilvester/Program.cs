﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TheRuleOfSilvester.Core;

namespace TheRuleOfSilvester
{
    internal class Program
    {
        private static Game game;
        private static InputComponent inputComponent;
        private static MultiplayerComponent multiplayerComponent;
        private static GameMenu menu;

        public static bool Running { get; private set; }

        private static string playerName;

        //┌┬┐└┴┘│├┼┤
        private static void Main(string[] args)
        {
            do
            {
                Console.Clear();
                //are = new AutoResetEvent(false);
                Console.OutputEncoding = Encoding.Unicode;
                Console.CursorVisible = false;
                inputComponent = new InputComponent();
                multiplayerComponent = new MultiplayerComponent();
                Running = true;

                menu = new GameMenu(new List<MenuItem>()
                {
                   new MenuItem(true, "New Game", SinglePlayer),
                   new MenuItem(false, "Multiplayer", MultiPlayer),
                   new MenuItem(false, "Exit", () => Running = false)
                });

                if (Running)
                {
                    var menuItem = menu.Run();
                    menuItem.Action();
                }

            } while (Running);
        }

        private static void MultiPlayer()
        {
            Console.Clear();

            do
            {
                Console.Write("Spielername: ");

                playerName = Console.ReadLine();
                Console.Clear();
            }
            while (string.IsNullOrWhiteSpace(playerName));

            Console.Write("IP Address of Server: ");

            bool GetAddress(string value, out IPAddress ipAddress)
            {
                //TODO: Port
                if (value.ToLower() == "localhorst" || string.IsNullOrWhiteSpace(value) || value.ToLower() == "horst")
                    value = "localhost";

                if (IPAddress.TryParse(value, out ipAddress))
                    return true;
                try
                {
                    ipAddress = Dns.GetHostAddresses(value)
                        .FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
                }
                catch { }

                return ipAddress != null;
            }

            IPAddress address;
            while (!GetAddress(Console.ReadLine(), out address))
            {
                Console.WriteLine("You've entered a wrong ip. Please try again! ☺");
                Console.Write("IP Address of Server: ");
            }
            Console.Clear();
            multiplayerComponent.Host = address.ToString();
            multiplayerComponent.Port = 4400;

            CreateGame(true);
        }

        private static void SinglePlayer()
        {
            Console.Clear();
            CreateGame(false);
        }

        private static void CreateGame(bool isMultiplayer)
        {
            using (game = new Game())
            {
                game.DrawComponent = new DrawComponent();
                game.InputCompoment = inputComponent;
                game.MultiplayerComponent = multiplayerComponent;
                game.Run(60, 60, isMultiplayer, playerName);
                inputComponent.Start();

                Console.CancelKeyPress += (s, e) => game.Stop();
                game.Wait();

                Console.Clear();
                inputComponent.Stop();

                if (game.Winners.Count > 0)
                {
                    Console.WriteLine("The winners are: ");
                    Console.WriteLine();
                    foreach (var winner in game.Winners)
                        Console.WriteLine(winner.Name);
                }

                Console.WriteLine();
                Console.WriteLine("Please press any key");
                Console.ReadKey();
            }

        }
    }
}
