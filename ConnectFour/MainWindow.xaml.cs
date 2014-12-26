using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        /// <summary>
        /// Type of the player.
        /// </summary>
        private enum PlayerType
        {
            [Description("Human")]
            Human = 0,
            [Description("Computer")]
            Computer = 1,
        }

        /// <summary>
        /// Status of the game.
        /// </summary>
        private enum GameStatus
        {
            [Description("Stopped")]
            Stopped = 0,
            [Description("Started")]
            Started = 1,
        }

        /// <summary>
        /// Type of the sound effect.
        /// </summary>
        private enum SoundType
        {
            [Description("Error")]
            Error = 0,
            [Description("Success")]
            Success = 1,
        }

        #endregion

        #region Constants

        /// <summary>
        /// Represents empty tile.
        /// </summary>
        private const char TileEmpty = '-';
        /// <summary>
        /// Represents tile for player/opponent one 'Human'.
        /// </summary>
        private const char TileHuman = '☺';
        /// <summary>
        /// Represents tile for player/opponent two 'Computer'.
        /// </summary>
        private const char TileComputer = '☻';
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
        /// <summary>
        /// Width of the game board.
        /// </summary>
        private const int BoardWidth = 7;
        /// <summary>
        /// Height of the game board.
        /// </summary>
        private const int BoardHeight = 6;
        /// <summary>
        /// How many connected tiles (horizontally, vertically or diaglonally) required to win.
        /// </summary>
        private const int TilesToWin = 4;

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
        /// Current state of the game board.
        /// </summary>
        private char[][] boardState;
        /// <summary>
        /// Current player who has to play.
        /// </summary>
        private PlayerType currentPlayer;
        /// <summary>
        /// Current status of the game (started/stopped).
        /// </summary>
        private GameStatus gameStatus;
        /// <summary>
        /// The tile of the current player.
        /// </summary>
        private char playerTile;
        /// <summary>
        /// Matrix of blocks to be used by players.
        /// </summary>
        private Ellipse[][] blocks;
        /// <summary>
        /// Brush with transparent color.
        /// </summary>
        private Brush transparentBrush;
        /// <summary>
        /// Brush with red color.
        /// </summary>
        private Brush redBrush;
        /// <summary>
        /// Bursh with blue color.
        /// </summary>
        private Brush blueBrush;

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
        /// Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            // stat or reset of the game
            if (gameStatus == GameStatus.Stopped)
            {
                groupBoxFirstPlayer.IsEnabled = false;
                buttonStart.Content = "■";
                buttonStart.ToolTip = "End the game.";
                StartGame();
                gameStatus = GameStatus.Started;
            }
            else if (gameStatus == GameStatus.Started)
            {
                groupBoxFirstPlayer.IsEnabled = true;
                buttonStart.Content = "►";
                buttonStart.ToolTip = "Start the game.";
                ResetGame();
                gameStatus = GameStatus.Stopped;
            }

            labelGameStatus.Content = gameStatus.Description();
        }

        /// <summary>
        /// Toggle the player to start the game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioButtonPlayer_Click(object sender, RoutedEventArgs e)
        {
            // change the current player
            object tagObject = ((ToggleButton)sender).Tag;
            string tagString = (string)tagObject;
            int tagInt = int.Parse(tagString);
            currentPlayer = (PlayerType)tagInt;

            labelCurrentPlayer.Content = currentPlayer.Description();
            ellipseCurrentPlayer.Fill = PlayerColor(currentPlayer);
        }

        /// <summary>
        /// Ellipse is clicked, add new block to the game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Ellipse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (currentPlayer == PlayerType.Computer)
            {
                PlaySound(SoundType.Error);
                return;
            }

            // if game not start and player is 'Human', then start the game
            // and place the first block
            if (gameStatus == GameStatus.Stopped)
            {
                ButtonStart_Click(sender, e);
            }

            // get the index of the block
            Ellipse ellipse = (Ellipse)sender;
            int row = int.Parse(ellipse.GetValue(Grid.RowProperty).ToString());
            int column = int.Parse(ellipse.GetValue(Grid.ColumnProperty).ToString()) - 1;

            // check if this is acceptable play
            // i.e there is an empty tile in the current clicked column
            bool isEmptyTile = false;
            for (int y = 0; y < BoardHeight; y++)
            {
                if (boardState[y][column] == TileEmpty)
                {
                    isEmptyTile = true;
                    break;
                }
            }

            if (!isEmptyTile)
            {
                PlaySound(SoundType.Error);
                return;
            }

            // fill in the current tile buy 'Human', switch player
            // and call the computer to play the next step
            for (int y = BoardHeight - 1; y > -1; y--)
            {
                if (boardState[y][column] == TileEmpty)
                {
                    blocks[y][column].Fill = PlayerColor(currentPlayer);
                    break;
                }
            }

            // switch the current player
            currentPlayer = PlayerType.Computer;
            ellipseCurrentPlayer.Fill = PlayerColor(currentPlayer);
            labelCurrentPlayer.Content = currentPlayer.Description();

            Utils.DebugLine("Row, Column: " + row + ", " + column);
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
            // setting the current player
            currentPlayer = PlayerType.Human;
            playerTile = TileHuman;

            // color brushes
            transparentBrush = new SolidColorBrush(Colors.Transparent);
            redBrush = new SolidColorBrush(Colors.Red);
            blueBrush = new SolidColorBrush(Colors.Blue);

            // list of the blocks (circles)
            blocks = new Ellipse[BoardHeight][]
            {
                new Ellipse[BoardWidth]{ellipseA6, ellipseB6, ellipseC6, ellipseD6, ellipseE6, ellipseF6, ellipseG6},
                new Ellipse[BoardWidth]{ellipseA5, ellipseB5, ellipseC5, ellipseD5, ellipseE5, ellipseF5, ellipseG5},
                new Ellipse[BoardWidth]{ellipseA4, ellipseB4, ellipseC4, ellipseD4, ellipseE4, ellipseF4, ellipseG4},
                new Ellipse[BoardWidth]{ellipseA3, ellipseB3, ellipseC3, ellipseD3, ellipseE3, ellipseF3, ellipseG3},
                new Ellipse[BoardWidth]{ellipseA2, ellipseB2, ellipseC2, ellipseD2, ellipseE2, ellipseF2, ellipseG2},
                new Ellipse[BoardWidth]{ellipseA1, ellipseB1, ellipseC1, ellipseD1, ellipseE1, ellipseF1, ellipseG1},
            };

            ResetGame();

            // event handlers for all the ellipses
            for (int y = 0; y < BoardHeight; y++)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    blocks[y][x].MouseDown += Ellipse_MouseDown;
                }
            }

            Utils.DebugLine("Initial State");
            Utils.DebugLine(FormatState(boardState));
        }

        /// <summary>
        /// Build string out of given game state.
        /// </summary>
        /// <param name="state"></param>
        private string FormatState(char[][] state)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("\n");

            for (int y = 0; y < BoardHeight; y++)
            {
                stringBuilder.Append("\n| ");

                for (int x = 0; x < BoardWidth; x++)
                {
                    stringBuilder.Append(String.Format("{0} ", state[y][x]));
                }

                stringBuilder.Append("|");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Switch the current player and his/here/its tile.
        /// </summary>
        private void SwitchPlayer()
        {
            if (currentPlayer == PlayerType.Human)
            {
                currentPlayer = PlayerType.Computer;
                playerTile = TileHuman;
            }
            else if (currentPlayer == PlayerType.Computer)
            {
                currentPlayer = PlayerType.Human;
                playerTile = TileComputer;
            }
            else
            {
                throw new Exception("Unknown player type in SwitchPlayer() method.");
            }
        }

        /// <summary>
        /// Get the color of the given player.
        /// </summary>
        /// <returns></returns>
        private Brush PlayerColor(PlayerType player)
        {
            return player == PlayerType.Human ? redBrush : blueBrush;
        }

        /// <summary>
        /// Start the game.
        /// </summary>
        private void StartGame()
        {
            // game stats
            //MiniMax(gameState, 6);
        }

        /// <summary>
        /// Reset the game.
        /// </summary>
        private void ResetGame()
        {
            // reset the tiles
            for (int y = 0; y < BoardHeight; y++)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    blocks[y][x].Fill = transparentBrush;
                }
            }

            // define initial state
            boardState = new char[BoardHeight][];
            for (int y = 0; y < BoardHeight; y++)
            {
                boardState[y] = new char[BoardWidth];
                for (int x = 0; x < BoardWidth; x++)
                {
                    boardState[y][x] = TileEmpty;
                }
            }

            // reset the counters
            nodes = 0;
            levels = 0;

            // game stats
            labelGameStatus.Content = gameStatus.Description();
            labelCurrentPlayer.Content = currentPlayer.Description();
            ellipseCurrentPlayer.Fill = PlayerColor(currentPlayer);
        }

        /// <summary>
        /// Play the give tye of sound.
        /// </summary>
        private void PlaySound(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.Error:
                    SystemSounds.Beep.Play();
                    break;
                case SoundType.Success:
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Search Methods

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

            for (int x = 0; x < BoardWidth; x++)
            {
                for (int y = BoardHeight - 1; y > -1; y--)
                {
                    if (state[y][x] == TileEmpty)
                    {
                        child = (char[][])state.Clone();
                        child[y][x] = playerTile;
                        children.Add(child);
                    }
                }
            }

            return children.ToArray();
        }

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
            for (int x = 0; x < BoardWidth - MaxWidth; x++)
            {

            }

            // look for n connected tiles vertically
            for (int y = BoardHeight; y > 0; y--)
            {

            }

            return isWinning;
        }

        #endregion
    }
}