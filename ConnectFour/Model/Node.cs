using System;

namespace ConnectFour
{
    public abstract class Node
    {
        public char[][] State { get; set; }
        public Node Parent { get; set; }
        public Node[] Children { get; set; }

        public Node()
        {

        }
                
        public Node(char[][] state, Node parent)
        {
            State = state;
            Parent = parent;
        }
    }
}
