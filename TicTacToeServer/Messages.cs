using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToeServer
{
    internal class Messages
    {
        public class Client
        {
            public const string Host = "_Host";
            public const string Join = "_Join";
            public const string Login = "_Login";
            public const string Move = "_Move";
            public const string EndGame = "_EndGame";
        }
        public class Server
        {
            public const string Start = "_Start";
            public const string Matches = "_Matches";
            public const string Cancel = "_Cancel";
            public const string Disconnect = "_Disconnect";
            public const string Logged = "_Logged";
            public const string Win = "_Win";
            public const string Lost = "_Lost";
        }
    }
}
