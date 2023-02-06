using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToeServer
{
    internal class Match
    {
        public Player playerO;
        public Player playerX;

        public Match(Player playerO)
        {
            this.playerO = playerO;
            this.playerX = null;
        }
    }
}
