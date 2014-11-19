using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConnectFour
{
    public class Level
    {
        public Node[] Nodes { get; set; }

        public Level(params Node[] nodes)
        {
            Nodes = nodes;
        }
    }
}