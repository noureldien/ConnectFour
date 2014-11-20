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
        #region Enums

        private enum Player
        {
            Human = 0,
            Computer = 1,
        };

        #endregion

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
        /// Counter for the number of nodes created in our current search.
        /// </summary>
        private int nodes;
        /// <summary>
        /// Counter for the number of levels reached in our current search.
        /// </summary>
        private int levels;
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
        private char[][] gameState;
        /// <summary>
        /// Current player who has to play.
        /// </summary>
        private Player player;
        /// <summary>
        /// The tile of the current player.
        /// </summary>
        private char playerTile;

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
            // setting game board variables
            width = 5;
            height = 4;
            tilesToWin = 3;

            // setting the current player
            player = Player.Human;
            playerTile = TileA;

            nodes = 0;
            levels = 0;

            // define initial state
            gameState = new char[height][];
            for (int y = 0; y < height; y++)
            {
                gameState[y] = new char[width];
                for (int x = 0; x < width; x++)
                {
                    gameState[y][x] = TileE;
                }
            }

            Utils.DebugLine("Initial State");
            Utils.DebugLine(FormatState(gameState));
        }

        /// <summary>
        /// Build string out of given game state.
        /// </summary>
        /// <param name="state"></param>
        private string FormatState(char[][] state)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("\n");

            for (int y = 0; y < height; y++)
            {
                stringBuilder.Append("\n| ");

                for (int x = 0; x < width; x++)
                {
                    stringBuilder.Append(String.Format("{0} ", state[y][x]));
                }

                stringBuilder.Append("|");
            }

            return stringBuilder.ToString();
        }

        #endregion

        #region Search Methods

        /// <summary>
        /// Switch the current player and his/here/its tile.
        /// </summary>
        private void SwitchPlayer()
        {
            if (player == Player.Human)
            {
                player = Player.Computer;
                playerTile = TileA;
            }
            else if (player == Player.Computer)
            {
                player = Player.Human;
                playerTile = TileB;
            }
            else
            {
                throw new Exception("Unknown player type in SwitchPlayer() method.");
            }
        }

        /// <summary>
        /// Breadth first search algorithm until reaching the given maxLevel. If maxLevel = 0, there is
        /// no level limtation on the algorithm.
        /// </summary>
        /// <param name="maxLevel"></param>
        private void MiniMax(char[][] initialState, int maxLevel = 0)
        {
            // input validation
            if (maxLevel < 0)
            {
                throw new ArgumentOutOfRangeException("Sorry, max level given to BreadthFirst() method can't be negative.");
            }

            Node initialNode = new Node(initialState, null);
            Level currentLevel = new Level(initialNode);
            List<Node> newLevelNodes = null;
            char[][][] newLevelData = null;

            // while the current level does not have a winning state (node with a winning state)
            // move to the next level
            while (currentLevel.Nodes.Count(i => IsWinning(i.State)) == 0
                && (maxLevel == 0 || (maxLevel > 0 && levels < maxLevel)))
            {
                levels++;
                newLevelNodes = new List<Node>();
                foreach (Node node in currentLevel.Nodes)
                {
                    newLevelData = PossibleChildren(node.State);
                    newLevelNodes.AddRange(newLevelData.Select(i => new Node(i, node)));

                    nodes += newLevelData.Length;
                }

                // create the new level
                currentLevel = new Level(newLevelNodes.ToArray());

                // for testing
                foreach (var n in currentLevel.Nodes)
                {
                    Utils.DebugLine("Level: " + levels);
                    Utils.DebugLine(FormatState(n.State));
                }
            }

            // check if solution was found
            //Node goalNode = currentLevel.Nodes.FirstOrDefault(i => EqualStates(i.Data, goalState));
            //bool isSolutionFound = goalNode != null;
            //if (isSolutionFound)
            //{
            //    PrintSolution(goalNode);
            //}
            //else
            //{
            //    PrintSolutionNotFound();
            //}
            //SearchFinished();
        }

        /// <summary>
        /// Get the possible children (next possible game states) for the current game state.
        /// </summary>
        private char[][][] PossibleChildren(char[][] state)
        {
            List<char[][]> children = new List<char[][]>();
            char[][] child = null;

            for (int x = 0; x < width; x++)
            {
                for (int y = height - 1; y > -1; y--)
                {
                    if (state[y][x] == TileE)
                    {
                        child = (char[][])state.Clone();
                        child[y][x] = playerTile;
                        children.Add(child);
                    }
                }
            }

            return children.ToArray();
        }

        int hi = 0;

        /// <summary>
        /// Weather is the given state is considered a winning one or not.
        /// </summary>
        /// <returns></returns>
        private bool IsWinning(char[][] state)
        {
            return false;

            bool isWinning = false;

            // look for n connected tiles horizontally
            // tilesToWin
            for (int x = 0; x < width - MaxWidth; x++)
            {

            }

            // look for n connected tiles vertically
            for (int y = height; y > 0; y--)
            {

            }

            return isWinning;
        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MiniMax(gameState, 6);
        }
    }
}