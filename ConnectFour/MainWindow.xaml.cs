using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
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
        /// Type of the search algorithm used.
        /// </summary>
        private enum SearchType
        {
            [Description("Minimax")]
            Minimax = 0,
            [Description("Alpha–beta")]
            AlphaBeta = 1,
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
        private const char TileE = '-';
        /// <summary>
        /// Represents tile for player/opponent one 'Human'.
        /// </summary>
        private const char TileH = '☺';
        /// <summary>
        /// Represents tile for player/opponent two 'Computer'.
        /// </summary>
        private const char TileC = '☻';
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
        /// Type of search to be used in the game.
        /// </summary>
        private SearchType searchType;
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
        /// <summary>
        /// thread used by the second player 'Computer' to do its game search
        /// and play the next step of the game.
        /// </summary>
        private Thread searchThread;
        /// <summary>
        /// Thread used to update the counters in the UI.
        /// </summary>
        private Thread infoThread;

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
                groupBoxSearchType.IsEnabled = false;
                groupBoxFirstPlayer.IsEnabled = false;
                buttonStart.Content = "■";
                buttonStart.ToolTip = "End the game.";
                gameStatus = GameStatus.Started;

                if (currentPlayer == PlayerType.Computer)
                {
                    NextStepComputer();
                }
            }
            else if (gameStatus == GameStatus.Started)
            {
                StopThreads();
                groupBoxGameBoard.IsEnabled = true;
                groupBoxSearchType.IsEnabled = false;
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
        /// Toggle the search algorithm used in the game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            // change the current player
            object tagObject = ((ToggleButton)sender).Tag;
            string tagString = (string)tagObject;
            int tagInt = int.Parse(tagString);
            searchType = (SearchType)tagInt;
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
                ButtonStart_Click(null, null);
            }

            // get the index of the block
            Ellipse ellipse = (Ellipse)sender;
            int row = int.Parse(ellipse.GetValue(Grid.RowProperty).ToString());
            int column = int.Parse(ellipse.GetValue(Grid.ColumnProperty).ToString()) - 1;

            // play the next step by 'Human'
            NextStepHuman(column);
        }

        /// <summary>
        /// Dispose objects and stop search threat (if working) before close.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            StopThreads();
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
        }

        /// <summary>
        /// Stop the search and info threads (if alive).
        /// </summary>
        private void StopThreads()
        {
            if (searchThread != null && searchThread.IsAlive)
            {
                searchThread.Abort();
            }
            if (infoThread != null && infoThread.IsAlive)
            {
                infoThread.Abort();
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

        /// <summary>
        /// Switch the current player to the given one, update the UI correspondingly.
        /// </summary>
        /// <param name="player"></param>
        private void SwitchPlayer(PlayerType player)
        {
            currentPlayer = player;

            Dispatcher.Invoke(() =>
            {
                ellipseCurrentPlayer.Fill = PlayerColor(currentPlayer);
                labelCurrentPlayer.Content = currentPlayer.Description();
            });
        }

        /// <summary>
        /// Keep invoking the UpdateInfo method every amout of time. Invoke using UI thread.
        /// </summary>
        private void UpdateCountersInvoker()
        {
            while (true)
            {
                Thread.Sleep(500);
                this.Dispatcher.Invoke(UpdateCounters);
            }
        }

        /// <summary>
        /// Update the counters of the game (num. of levels and num. of nodes).
        /// </summary>
        private void UpdateCounters()
        {
            labelGameLevels.Content = String.Format("{0:0,0}", levels);
            labelGameNodes.Content = String.Format("{0:0,0}", nodes);

            //TimeSpan duration = ((DateTime.Now - startTime) + processDuration);
            //labelTime.Content = String.Format("{0:00}:{1:00}", duration.TotalMinutes, duration.Seconds);
        }

        /// <summary>
        /// Build string out of given game state.
        /// </summary>
        /// <param name="state"></param>
        private string FormatState(char[][] state)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int y = 0; y < BoardHeight; y++)
            {
                stringBuilder.Append("| ");

                for (int x = 0; x < BoardWidth; x++)
                {
                    stringBuilder.Append(String.Format("{0} ", state[y][x]));
                }

                stringBuilder.Append("|\n");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Format the branch as string; the barnch that ends with the given node. Bottom up.
        /// </summary>
        private string FormatBranch(Node node)
        {
            int steps = 0;
            StringBuilder stringBuilder = new StringBuilder();
            while (node != null)
            {
                steps++;
                stringBuilder.Append(FormatState(node.State));
                stringBuilder.Append("\n");
                node = node.Parent;
            }
            
            string result = stringBuilder.ToString();
            return result;
        }

        #endregion

        #region Search Methods

        /// <summary>
        /// Play the next step in the game by the first player 'Human'.
        /// </summary>
        /// <param name="column"></param>
        private void NextStepHuman(int column)
        {
            // check if this is acceptable play
            // i.e there is an empty tile in the current clicked column
            bool isEmptyTile = false;
            for (int y = 0; y < BoardHeight; y++)
            {
                if (boardState[y][column] == TileE)
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

            // fill in the current tile buy 'Human'
            for (int y = BoardHeight - 1; y > -1; y--)
            {
                if (boardState[y][column] == TileE)
                {
                    boardState[y][column] = TileH;
                    blocks[y][column].Fill = PlayerColor(currentPlayer);
                    break;
                }
            }

            // check if the game won
            if (IsWinning(boardState))
            {
                string message = "Game ended, you won!";
                FinishGame(message);
                return;
            }

            // check if the game ended
            if (IsComplete(boardState))
            {
                string message = "Game ended with tie!";
                FinishGame(message);
                return;
            }

            // switch the current player
            // and call the computer to play the next step
            SwitchPlayer(PlayerType.Computer);
            NextStepComputer();
        }

        /// <summary>
        /// Play the next step on the game by the second player 'Computer'.
        /// </summary>
        private void NextStepComputer()
        {
            // reset counters and update UI
            levels = 0;
            nodes = 0;
            UpdateCounters();

            // initialize the search and info threads.
            infoThread = new Thread(UpdateCountersInvoker);
            searchThread = new Thread(SearchInvoker);

            // start the threads
            searchThread.Start();
            infoThread.Start();
        }

        /// <summary>
        /// Calls the suitable search algorithm according to the users preferences.
        /// </summary>
        private void SearchInvoker()
        {
            int column = -1;
            if (searchType == SearchType.Minimax)
            {
                column = Minimax(boardState);
            }
            else if (searchType == SearchType.AlphaBeta)
            {
                column = AlphaBeta(boardState);
            }

            // stop the info thread
            if (infoThread != null && infoThread.IsAlive)
            {
                infoThread.Abort();
            }
            Dispatcher.Invoke(() =>
            {
                UpdateCounters();
            });

            // check if the search algorithm didn't find a column
            if (column == -1)
            {
                ButtonStart_Click(null, null);
                MessageBox.Show("No column found by the search algorithm!", "Game", MessageBoxButton.OK);
                return;
            }

            // fill in the current tile buy 'Computer', switch player
            // and let 'Human' play the next step
            for (int y = BoardHeight - 1; y > -1; y--)
            {
                if (boardState[y][column] == TileE)
                {
                    boardState[y][column] = TileC;
                    Dispatcher.Invoke(() =>
                    {
                        blocks[y][column].Fill = PlayerColor(currentPlayer);
                    });
                    break;
                }
            }

            // check if the game won
            if (IsWinning(boardState))
            {
                string message = "Game ended, computer won!";
                FinishGame(message);
                return;
            }

            // check if the game ended
            if (IsComplete(boardState))
            {
                string message = "Game ended with tie!";
                FinishGame(message);
                return;
            }

            // switch the current player
            SwitchPlayer(PlayerType.Human);
        }

        /// <summary>
        /// Return the adviced column to place the tile in using dummy search algorithm.
        /// <para>
        /// Return -1 if no column with empty tiles found.
        /// </para>
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private int Dummy(char[][] state)
        {
            int column = -1;
            bool emptyColumn = false;
            Random random = new Random();

            // make a guess about a column
            // then check if it contains empty tiles
            while (!emptyColumn)
            {
                column = random.Next(0, BoardWidth);
                for (int y = BoardHeight - 1; y > -1; y--)
                {
                    if (state[y][column] == TileE)
                    {
                        return column;
                    }
                }
                System.Threading.Thread.Sleep(10);
            }

            return column;
        }

        /// <summary>
        /// Return the adviced column to place the tile in using minimax search algorithm.
        /// <para>
        /// Return -1 if no column with empty tiles found.
        /// </para>
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private int Minimax(char[][] state)
        {
            const int maxLevel = 8;

            // reset counters
            nodes = 0;
            levels = 0;

            PlayerType levelPlayer = currentPlayer;
            MinimaxNode initialNode = new MinimaxNode(state, null, levelPlayer);
            Level currentLevel = new Level(initialNode);
            List<MinimaxNode> newLevelNodes = null;
            char[][][] newLevelData = null;

            // if the current level has nodes and the number of levels
            // didn't exceed the max, continue looping (move to the next level)
            while (currentLevel.Nodes.Length > 0 && levels < maxLevel)
            {
                // increment levels counter and choose the other player
                // as the player of the new level
                levels++;
                levelPlayer = (levelPlayer == PlayerType.Human) ? PlayerType.Computer : PlayerType.Human;

                // loop on the nodes of the current level, get their children
                // get children only if the node does is not complete nor winning
                newLevelNodes = new List<MinimaxNode>();
                foreach (MinimaxNode node in currentLevel.Nodes)
                {
                    // check if the node is complete
                    if (IsComplete(node.State))
                    {
                        Utils.DebugLine("Found complete node, level: " + levels);
                        continue;
                    }

                    // check if the node is winning
                    if (IsWinning(node.State))
                    {
                        Utils.DebugLine("Found winning node, level: " + levels);
                        Utils.DebugLine(FormatBranch(node));
                        return 4;
                        node.Value = node.Player == PlayerType.Human ? -1 : 1;
                        continue;
                    }

                    newLevelData = ChildrenStates(node.State, levelPlayer);
                    newLevelNodes.AddRange(newLevelData.Select(i => new MinimaxNode(i, node, levelPlayer)));

                    // increment nodes counter
                    nodes += newLevelData.Length;
                }

                // create the new level
                currentLevel = new Level(newLevelNodes.ToArray());

                //// for testing
                //foreach (MinimaxNode n in currentLevel.Nodes)
                //{
                //    Utils.DebugLine("\nLevel: " + levels + ", Value: " + n.Value + ", Player: " + n.Player.Description());
                //    Utils.DebugLine(FormatState(n.State));
                //}
            }

            int column = 5;
            return column;
        }

        /// <summary>
        /// Return the adviced column to place the tile in using alpha-beta pruning search algorithm.
        /// </summary>
        /// <para>
        /// Return -1 if no column with empty tiles found.
        /// </para>
        /// <param name="state"></param>
        /// <returns></returns>
        private int AlphaBeta(char[][] state)
        {
            return -1;
        }

        /// <summary>
        /// Weather is the given state is considered a winning one or not.
        /// </summary>
        /// <returns></returns>
        private bool IsWinning(char[][] state)
        {
            int count1 = 0;
            int count2 = 0;

            for (int y = 0; y < BoardHeight; y++)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    #region 1. look for n connected tiles horizontally

                    // 1.1 right to the current
                    if (x + TilesToWin - 1 < BoardWidth)
                    {
                        count1 = 0;
                        count2 = 0;
                        for (int i = x; i < x + TilesToWin; i++)
                        {
                            if (state[y][i] == TileH)
                            {
                                count1++;
                            }
                            else if (state[y][i] == TileC)
                            {
                                count2++;
                            }
                        }

                        if (count1 == TilesToWin || count2 == TilesToWin)
                        {
                            return true;
                        }
                    }

                    // 1.2 left to the current
                    if (x - TilesToWin + 1 > -1)
                    {
                        count1 = 0;
                        count2 = 0;
                        for (int i = x; i > x - TilesToWin; i--)
                        {
                            if (state[y][i] == TileH)
                            {
                                count1++;
                            }
                            else if (state[y][i] == TileC)
                            {
                                count2++;
                            }
                        }

                        if (count1 == TilesToWin || count2 == TilesToWin)
                        {
                            return true;
                        }
                    }

                    #endregion

                    #region 2. look for n connected tiles vertically

                    // 2.1 buttom of the current
                    if (y + TilesToWin - 1 < BoardHeight)
                    {
                        count1 = 0;
                        count2 = 0;
                        for (int i = y; i < y + TilesToWin; i++)
                        {
                            if (state[i][x] == TileH)
                            {
                                count1++;
                            }
                            else if (state[i][x] == TileC)
                            {
                                count2++;
                            }
                        }

                        if (count1 == TilesToWin || count2 == TilesToWin)
                        {
                            return true;
                        }
                    }

                    // 2.2 top of the current
                    if (y - TilesToWin + 1 > -1)
                    {
                        count1 = 0;
                        count2 = 0;
                        for (int i = y; i > y - TilesToWin; i--)
                        {
                            if (state[i][x] == TileH)
                            {
                                count1++;
                            }
                            else if (state[i][x] == TileC)
                            {
                                count2++;
                            }
                        }

                        if (count1 == TilesToWin || count2 == TilesToWin)
                        {
                            return true;
                        }
                    }

                    #endregion

                    #region 3. look for n connected tiles diagonally

                    #endregion
                }
            }

            return false;
        }

        /// <summary>
        /// Check if game board is complete and no place for next step (no empty tiles).
        /// </summary>
        /// <returns></returns>
        private bool IsComplete(char[][] state)
        {
            for (int x = 0; x < BoardWidth; x++)
            {
                if (state[0][x] == TileE)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get the possible children (next possible game states) for the current game state.
        /// </summary>
        private char[][][] ChildrenStates(char[][] parentState, PlayerType player)
        {
            char tile = player == PlayerType.Human ? TileH : TileC;
            List<char[][]> children = new List<char[][]>();
            char[][] child = null;

            for (int x = 0; x < BoardWidth; x++)
            {
                for (int y = BoardHeight - 1; y > -1; y--)
                {
                    if (parentState[y][x] == TileE)
                    {
                        child = CloneState(parentState);
                        child[y][x] = tile;
                        children.Add(child);
                        break;
                    }
                }
            }

            return children.ToArray();
        }

        /// <summary>
        /// Clone the given state.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private char[][] CloneState(char[][] state)
        {
            char[][] newState = new char[BoardHeight][];

            for (int y = 0; y < BoardHeight; y++)
            {
                newState[y] = new char[BoardWidth];
                for (int x = 0; x < BoardWidth; x++)
                {
                    newState[y][x] = state[y][x];
                }
            }

            return newState;
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
                    boardState[y][x] = TileE;
                }
            }

            // reset the counters
            nodes = 0;
            levels = 0;

            // game stats
            currentPlayer = (bool)radioButtonHuman.IsChecked ? PlayerType.Human : PlayerType.Computer;
            labelGameStatus.Content = gameStatus.Description();
            labelCurrentPlayer.Content = currentPlayer.Description();
            ellipseCurrentPlayer.Fill = PlayerColor(currentPlayer);
        }

        /// <summary>
        /// Display a message declaring the end of the game and the result.
        /// </summary>
        private void FinishGame(string message)
        {
            Dispatcher.Invoke(() =>
            {
                groupBoxGameBoard.IsEnabled = false;
                MessageBox.Show(message, "Game", MessageBoxButton.OK);
            });
        }

        #endregion
    }
}