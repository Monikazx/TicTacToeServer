using SimpleTcp;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace TicTacToeServer
{
    class Program
    {
        public static List<Player> connectedClients = new List<Player>();
        public static List<Match> matches = new List<Match>();
        static SimpleTcpServer server = new SimpleTcpServer("127.0.0.1:8000");
        public static DatabaseTicTacToeDataContext db = new DatabaseTicTacToeDataContext($@"Data Source = 
        (LocalDB)\MSSQLLocalDB; AttachDbFilename = {Environment.CurrentDirectory}\DatabaseTicTacToe.mdf; Integrated Security = True");


        static void Main(string[] args)
        {
            server.Events.ClientConnected += Events_ClientConnected;
            server.Events.ClientDisconnected += Events_ClientDisconnected;
            server.Events.DataReceived += Events_DataReceived;
            server.Start();
            Console.WriteLine("Serwer has started. ");

            string inputData;
            while((inputData = Console.ReadLine()) != "EXIT")
            {
                if (inputData.Equals("Client"))
                {
                    foreach(Player player in connectedClients)
                    {
                        Console.WriteLine(player.ip);
                    }
                }
            }   
        }

        private static void Events_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine($"New Client connected {e.IpPort}");
        }
        private static void Events_ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            Console.WriteLine($"Client disconnected {e.IpPort}");
            connectedClients.RemoveAll(x => x.ip == e.IpPort);
        }
        private static void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"Data from client: {e.IpPort} - {Encoding.UTF8.GetString(e.Data)}");
            string[] messageData = Encoding.UTF8.GetString(e.Data).Split(':');

            switch(messageData[0])
            {
                case Messages.Client.Host:
                    matches.Add(new Match(connectedClients.Find(x => x.ip == e.IpPort)));
                    Console.WriteLine($"Player: {e.IpPort} created new game.");
                    break;
                case Messages.Client.Join:
                    string host = messageData[1];
                    foreach(Match match in matches)
                    {
                        if(match.playerO.name.Equals(host) && match.playerX == null)
                        {
                            match.playerX = new Player(e.IpPort, connectedClients.Find(x => x.ip == e.IpPort).name);
                            server.Send(match.playerO.ip, Messages.Server.Start + ":PlayerO");
                            server.Send(match.playerX.ip, Messages.Server.Start + ":PlayerX");
                            SendMatchesToAll();
                            Console.WriteLine("Match has started");
                            break;
                        }
                    }
                    break;
                case Messages.Server.Cancel:
                    matches.RemoveAll(x => x.playerO.ip == e.IpPort);
                    SendMatchesToAll();
                    break;
                case Messages.Server.Matches:
                    server.Send(e.IpPort, listOfMatches());
                    break;
                case Messages.Client.Login:
                    if (!db.userLoginExists(messageData[1], messageData[2])){
                        server.Send(e.IpPort, Messages.Server.Disconnect);
                        break;
                    }
                    if (connectedClients.Exists(x => x.name == messageData[1]))
                    {
                        server.Send(e.IpPort, Messages.Server.Logged);
                    }
                    connectedClients.Add(new Player(e.IpPort, messageData[1]));
                    break;
                case Messages.Client.Move:
                    Match oneMatch = matches.Find(x => x.playerX.ip == e.IpPort);
                    if (oneMatch == null)
                    {
                        oneMatch = matches.Find(x => x.playerO.ip == e.IpPort);
                    }

                    if (e.IpPort == oneMatch.playerO.ip)
                    {
                        server.Send(oneMatch.playerX.ip, Encoding.UTF8.GetString(e.Data));
                    }
                    else
                    {
                        server.Send(oneMatch.playerO.ip, Encoding.UTF8.GetString(e.Data));
                    }
                    break;
                case Messages.Client.EndGame:  // player that won
                    Match thisMatch = matches.Find(x => x.playerO.ip == e.IpPort);
                    if (thisMatch == null)
                    {
                        thisMatch = matches.Find(x => x.playerX.ip == e.IpPort);
                    }
                    Player finalPlayerO = connectedClients.Find(x => x.ip == thisMatch.playerO.ip);
                    Player finalPlayerX = connectedClients.Find(x => x.ip == thisMatch.playerX.ip);
                    Player playerWinner;
                    if (messageData[1] == "PlayerO")
                    {
                        playerWinner = finalPlayerO;
                        server.Send(e.IpPort, Messages.Server.Win + ":" + finalPlayerO.name + ":" +
                            finalPlayerX.name + ":" + finalPlayerO.name);
                        // send to player name of winner
                        server.Send(thisMatch.playerX.ip, Messages.Server.Lost + ":" + finalPlayerO.name);
                    }
                    else
                    {
                        playerWinner = finalPlayerX;
                        server.Send(e.IpPort, Messages.Server.Win + ":" + finalPlayerO.name + ":" +
                            finalPlayerX.name + ":" + finalPlayerX.name);
                        server.Send(thisMatch.playerO.ip, Messages.Server.Lost + ":" + finalPlayerX.name);
                    }

                    // SAVING MATCH
                    addGameToDB(finalPlayerO.name + ":" + finalPlayerX.name + ":" + playerWinner.name);
                    break;
                default:
                    break;
            }
        }
        private static string listOfMatches()
        {
            StringBuilder allMatches = new StringBuilder();
            allMatches.Append(Messages.Server.Matches);

            if (allMatches.Length == 0)
            {
                return null;
            }
            foreach(Match match in matches)
            {
                if(match.playerX == null)
                {
                    allMatches.Append($":{match.playerO.name}");  // active hosts
                }
            }
            return allMatches.ToString();
                
        }

        private static void SendMatchesToAll()
        {
            foreach(Player player in connectedClients)
            {
                if (!matches.Any(X => X.playerO.ip.Equals(player.ip) && X.playerX != null)) // send to playerO
                {
                    server.Send(player.ip, listOfMatches());
                    continue;
                }
                if (matches.Where(x => x.playerX != null).Any(x => x.playerX.ip.Equals(player.ip))) // send to playerX
                {
                    server.Send(player.ip, listOfMatches());
                }
            }
        }

        private static void addGameToDB(String gameData)
        {
            //playerO:playerX:playerWinner
            Game newGame = new Game();
            String[] data = gameData.Split(':');
            Console.WriteLine("Saving match: " + gameData);
            Console.WriteLine(data[0]+" "+data[1] + " "+data[2] + " ");

            foreach (User user in db.Users)
            {
                //Console.WriteLine("??"+user.name+"??");
                if (user.name.Replace(" ", "") == data[0])
                {
                    newGame.player1 = user.Id;
                    
                }
                if (user.name.Replace(" ", "") == data[1])
                {
                    newGame.player2 = user.Id;
                }
                if (user.name.Replace(" ", "") == data[2])
                {
                    newGame.winner = user.Id;
                }
            }
            Console.WriteLine("Saved: " + newGame.player1 +" "+ newGame.player2 + " " + newGame.winner);
            db.Games.InsertOnSubmit(newGame);
            try
            {
                db.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error with sumbiting changes");
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }



    }
}
