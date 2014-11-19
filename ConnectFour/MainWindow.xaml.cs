using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ConnectFour
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants

        /// <summary>
        /// Represents empty tile.
        /// </summary>
        private const char TileE = '-';
        /// <summary>
        /// Represents tile for player/opponent one.
        /// </summary>
        private const char TileA = '☺';
        /// <summary>
        /// Represents tile for player/opponent two.
        /// </summary>
        private const char TileB = '☻';
        /// <summary>
        /// Min width for the game board.
        /// </summary>
        private const int MinBoardWidth = 4;
        /// <summary>
        /// Min height for the game board.
        /// </summary>
        private const int MinBoardHeight = 4;
        /// <summary>
        /// Min steps-to-win for the game.
        /// </summary>
        private const int MinTilesToWin = 3;

        #endregion

        #region Variables

        /// <summary>
        /// Width of the game board.
        /// </summary>
        private int width;
        /// <summary>
        /// Height of the game board.
        /// </summary>
        private int height;
        /// <summary>
        /// How many connected tiles (horizontally, vertically or diaglonally) required to win.
        /// </summary>
        private int tilesToWin;
        /// <summary>
        /// Current state of the game board.
        /// </summary>
        private char[][] state;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Load game objects.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
        }

        /// <summary>
        /// Dispose objects and stop search threat (if working) before close.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize the game.
        /// </summary>
        private void Initialize()
        {
            width = 5;
            height = 4;
            tilesToWin = 3;

            state = new char[height][];
            for (int y = 0; y < height; y++)
            {
                state[y] = new char[width];
                for (int x = 0; x < width; x++)
                {
                    state[y][x] = TileE;
                }
            }

            PrintState(state);
        }

        /// <summary>
        /// Print the board of the game.
        /// </summary>
        private void PrintState(char[][] state)
        {

        }

        #endregion

        #region Search Methods

        /// <summary>
        /// Breadth first search algorithm until reaching the given maxLevel. If maxLevel = 0, there is
        /// no level limtation on the algorithm.
        /// </summary>
        /// <param name="maxLevel"></param>
        private void BreadthFirst(char[][] initialState, int maxLevel = 0)
        {
            // input validation
            if (maxLevel < 0)
            {
                throw new ArgumentOutOfRangeException("Sorry, max level given to BreadthFirst() method can't be negative.");
            }

            Node initialNode = new Node(initialState, null);
            Level currentLevel = new Level(initialNode);
            List<Node> newLevelNodes = null;
            char[][,] newLevelData = null;

            // while the current level does not has the goal node, move to the next level
            while (currentLevel.Nodes.Count(i => EqualStates(i.Data, goalState)) == 0
                && (maxLevel == 0 || (maxLevel > 0 && levels < maxLevel)))
            {
                levels++;
                newLevelNodes = new List<Node>();
                foreach (Node node in currentLevel.Nodes)
                {
                    newLevelData = PossibleChildren(node);
                    newLevelNodes.AddRange(newLevelData.Select(i => new Node(i, node)));

                    nodes += newLevelData.Length;
                }

                // create the new level
                currentLevel = new Level(newLevelNodes.ToArray());
            }

            // check if solution was found
            Node goalNode = currentLevel.Nodes.FirstOrDefault(i => EqualStates(i.Data, goalState));
            bool isSolutionFound = goalNode != null;
            if (isSolutionFound)
            {
                PrintSolution(goalNode);
            }
            else
            {
                PrintSolutionNotFound();
            }
            SearchFinished();
        }

        #endregion
    }
}