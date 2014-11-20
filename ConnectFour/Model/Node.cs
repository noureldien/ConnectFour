using System;

namespace ConnectFour
{
    public class Node
    {
        public char[][] State { get; set; }
        public Node Parent { get; set; }
        public Node[] Children { get; set; }
        public int MinMax { get; set; }
                
        public Node(char[][] state, Node parent)
        {
            State = state;
            Parent = parent;
        }
    }
}
