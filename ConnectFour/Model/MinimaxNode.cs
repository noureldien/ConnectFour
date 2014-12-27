using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectFour
{
    public class MinimaxNode : Node
    {
        public PlayerType Player { get; private set; }
        public int Value { get; set; }

        public MinimaxNode(char[][] state, Node parent, PlayerType player)
        {
            State = state;
            Parent = parent;
            Player = player;
        }
    }
}
