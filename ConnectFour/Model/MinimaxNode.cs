using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectFour
{
    public class MinimaxNode
    {
        public char[][] State { get; set; }
        public MinimaxNode Parent { get; set; }
        public MinimaxNode[] Children { get; set; }
        public int Value { get; set; }

        public MinimaxNode(char[][] state, MinimaxNode parent)
        {
            State = state;
            Parent = parent;
        }
    }
}
