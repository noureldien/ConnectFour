using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectFour
{
    public class MinimaxLevel
    {
        public MinimaxNode[] Nodes { get; set; }
        public bool IsMax { get; private set; }

        public MinimaxLevel(bool isMax, params MinimaxNode[] nodes)
        {
            Nodes = nodes;
            IsMax = isMax;
        }
    }
}
