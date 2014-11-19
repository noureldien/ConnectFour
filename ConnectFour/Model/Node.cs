using System;

namespace ConnectFour
{
    public class Node
    {
        public char[][] State { get; set; }
        public Node Parent { get; set; }
        public Node[] Children { get; set; }
        public bool Visited { get; set; }

        private Node()
        {
            Visited = false;
        }
        
        public Node(char[][] state, Node parent)
            : this()
        {
            State = state;
            Parent = parent;
        }
    }
}
