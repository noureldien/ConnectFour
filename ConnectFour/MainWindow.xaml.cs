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
using System.Windows.Media.Animation;
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
        private const char TileH = 'x';
        /// <summary>
        /// Represents tile for player/opponent two 'Computer'.
        /// </summary>
        private const char TileC = 'o';

        #endregion

        #region Variables

        /// <summary>
        /// Width of the game board.
        /// </summary>
        private int boardWidth;
        /// <summary>
        /// Height of the game board.
        /// </summary>
        private int boardHeight;
        /// <summary>
        /// How many connected tiles (horizontally, vertically or diaglonally) required to win.
        /// </summary>
        private int tilesToWin;
        /// <summary>
        /// Counter for the number of nodes created in our current search.
        /// </summary>
        private int nodeCount;
        /// <summary>
        /// Counter for the number of levels reached in our current search.
        /// </summary>
        private int levelCount;
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
        private SolidColorBrush redBrush;
        /// <summary>
        /// Bursh with blue color.
        /// </summary>
        private SolidColorBrush blueBrush;
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
                ResetGame();
                groupBoxGameBoard.IsEnabled = true;
                groupBoxSearchType.IsEnabled = false;
                groupBoxFirstPlayer.IsEnabled = false;
                groupBoxOptions.IsEnabled = false;
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
                groupBoxSearchType.IsEnabled = true;
                groupBoxFirstPlayer.IsEnabled = true;
                groupBoxOptions.IsEnabled = true;
                buttonStart.Content = "►";
                buttonStart.ToolTip = "Start the game.";
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
        /// Change width of the game board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SliderBoardWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            boardWidth = (int)e.NewValue;
            InitializeGameBoard();
            ResetGame();
        }

        /// <summary>
        /// Change height of the game board.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SliderBoardHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            boardHeight = (int)e.NewValue;
            InitializeGameBoard();
            ResetGame();
        }

        /// <summary>
        /// Change number of tiles required to win.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SliderTilesToWin_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tilesToWin = (int)e.NewValue;
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
            currentPlayer = PlayerType.Computer;

            // color brushes
            transparentBrush = new SolidColorBrush(Colors.Transparent);
            redBrush = new SolidColorBrush(Colors.Red);
            blueBrush = new SolidColorBrush(Colors.Blue);

            // get board variables before initializing the game board
            boardWidth = (int)sliderBoardWidth.Value;
            boardHeight = (int)sliderBoardHeight.Value;
            tilesToWin = (int)sliderTilesToWin.Value;

            InitializeGameBoard();
            ResetGame();

            // event handlers
            sliderBoardHeight.ValueChanged += SliderBoardHeight_ValueChanged;
            sliderBoardWidth.ValueChanged += SliderBoardWidth_ValueChanged;
            sliderTilesToWin.ValueChanged += SliderTilesToWin_ValueChanged;
        }

        /// <summary>
        /// Initialize game board.
        /// </summary>
        private void InitializeGameBoard()
        {
            gridGameBoard.Children.Clear();
            gridGameBoard.ColumnDefinitions.Clear();
            gridGameBoard.RowDefinitions.Clear();

            char[] characters = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
            Style styleEllipse = (Style)FindResource("StyleEllipse");

            Style styleLabelRowNames = (Style)FindResource("StyleLabelRowNames");
            Style styleBorderRowNames = (Style)FindResource("StyleBorderRowNames");
            Style styleLabelColumnNames = (Style)FindResource("StyleLabelColumnNames");
            Style styleBorderColumnNames = (Style)FindResource("StyleBorderColumnNames");

            // create ellipses
            Ellipse ellipse;
            blocks = new Ellipse[boardHeight][];
            for (int y = 0; y < boardHeight; y++)
            {
                blocks[y] = new Ellipse[boardWidth];
                for (int x = 0; x < boardWidth; x++)
                {
                    ellipse = new Ellipse();
                    ellipse.SetValue(Grid.RowProperty, y);
                    ellipse.SetValue(Grid.ColumnProperty, x + 1);
                    ellipse.Style = styleEllipse;
                    ellipse.MouseDown += Ellipse_MouseDown;

                    gridGameBoard.Children.Add(ellipse);
                    blocks[y][x] = ellipse;
                }
            }

            // add column and row definitions
            gridGameBoard.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(26, GridUnitType.Pixel),
            });
            for (int x = 0; x < boardWidth; x++)
            {
                gridGameBoard.ColumnDefinitions.Add(new ColumnDefinition()
                {
                    Width = new GridLength(1, GridUnitType.Star),
                });
            }
            for (int y = 0; y < boardHeight; y++)
            {
                gridGameBoard.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(1, GridUnitType.Star),
                });
            }
            gridGameBoard.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(26, GridUnitType.Pixel),
            });

            // create column names and row names
            Border border;
            for (int x = 0; x < boardWidth; x++)
            {
                border = new Border();
                border.Child = new Label()
                {
                    Content = characters[x],
                    Style = styleLabelColumnNames,
                };
                border.Style = styleBorderColumnNames;
                border.SetValue(Grid.ColumnProperty, x + 1);
                border.SetValue(Grid.RowProperty, boardHeight);

                gridGameBoard.Children.Add(border);
            }
            for (int y = 0; y < boardHeight; y++)
            {
                border = new Border();
                border.Child = new Label()
                {
                    Content = boardHeight - y,
                    Style = styleLabelRowNames,
                };
                border.Style = styleBorderRowNames;
                border.SetValue(Grid.RowProperty, y);
                border.SetValue(Grid.ColumnProperty, 0);

                gridGameBoard.Children.Add(border);
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
        private SolidColorBrush PlayerColor(PlayerType player)
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
            labelGameLevels.Content = String.Format("{0:0,0}", levelCount);
            labelGameNodes.Content = String.Format("{0:0,0}", nodeCount);

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

            for (int y = 0; y < boardHeight; y++)
            {
                stringBuilder.Append("| ");

                for (int x = 0; x < boardWidth; x++)
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
        private string FormatBranch(MinimaxNode node)
        {
            int steps = 0;
            StringBuilder stringBuilder = new StringBuilder();
            while (node.Parent != null)
            {
                steps++;
                stringBuilder.Append(FormatState(node.State));
                stringBuilder.Append("\n");
                node = node.Parent;
            }

            string result = stringBuilder.ToString();
            return result;
        }

        /// <summary>
        /// Print the search tree.
        /// </summary>
        /// <param name="levels"></param>
        private void PrintSearchTree(List<MinimaxLevel> levels)
        {
            // for testing
            StringBuilder stBuilder = new StringBuilder();
            stBuilder.Append("Max Print: 1K nodes\n-----------\n");
            int n = 0;
            int nMax = 1 * 1000;
            for (int i = 0; i < levels.Count && n < nMax; i++)
            {
                for (int j = 0; j < levels[i].Nodes.Length && n < nMax; j++)
                {
                    n++;
                    stBuilder.AppendFormat("\nLevel: {0}, Value: {1}\n{2}", (i + 1), levels[i].Nodes[j].Value, FormatState(levels[i].Nodes[j].State));
                }
            }

            string output = stBuilder.ToString();
            Dispatcher.Invoke(() =>
            {
                textBoxOutput.Text = output;
            });
        }

        /// <summary>
        /// Set the given color to the given ellipse, animated.
        /// </summary>
        /// <param name="ellipse"></param>
        /// <param name="brush"></param>
        private void SetTileColor(Ellipse ellipse, PlayerType player)
        {
            Color color = player == PlayerType.Human ? Colors.Red : Colors.Blue;
            SolidColorBrush brush = new SolidColorBrush(color);

            ColorAnimation animation = new ColorAnimation();
            animation.From = Colors.Transparent;
            animation.To = color;
            animation.Duration = new Duration(TimeSpan.FromMilliseconds(250));
            animation.EasingFunction = new CubicEase()
            {
                EasingMode = EasingMode.EaseIn,
            };
            ellipse.Fill = brush;
            ellipse.Fill.BeginAnimation(SolidColorBrush.ColorProperty, animation);
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
            for (int y = 0; y < boardHeight; y++)
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
            for (int y = boardHeight - 1; y > -1; y--)
            {
                if (boardState[y][column] == TileE)
                {
                    boardState[y][column] = TileH;
                    SetTileColor(blocks[y][column], currentPlayer);
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
            levelCount = 0;
            nodeCount = 0;
            UpdateCounters();

            // initialize the search and info threads.
            searchThread = new Thread(SearchInvoker);
            infoThread = new Thread(UpdateCountersInvoker);

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
                column = Dummy(boardState);
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
                string message = "Algorithm couldn't find a column, it returned -1";
                FinishGame(message);
                return;
            }

            Utils.DebugLine("Algorithm column: " + column);

            // fill in the current tile buy 'Computer', switch player
            // and let 'Human' play the next step
            for (int y = boardHeight - 1; y > -1; y--)
            {
                if (boardState[y][column] == TileE)
                {
                    boardState[y][column] = TileC;
                    Dispatcher.Invoke(() =>
                    {
                        SetTileColor(blocks[y][column], currentPlayer);
                    });
                    Utils.DebugLine("Filled tile (x, y): " + column + ", " + y);
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
                column = random.Next(0, boardWidth);
                for (int y = boardHeight - 1; y > -1; y--)
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
            // check if initial state, choose arbitrary column
            if (IsInitial(state))
            {
                List<int> cols = new List<int>();
                for (int i = 0; i < boardWidth; i++)
                {
                    cols.Add(i);
                }
                return GetRandomColum(cols);
            }

            // reset counters
            nodeCount = 0;
            levelCount = 0;
            int maxLevels = MaxLevel();

            bool isMax = (currentPlayer == PlayerType.Computer);
            int minimaxValue;
            char nextLevelTile;

            MinimaxNode initialNode = new MinimaxNode(state, null);
            List<MinimaxLevel> levels = new List<MinimaxLevel>(maxLevels);
            List<MinimaxNode> newLevelNodes = null;
            char[][][] newLevelData = null;

            // initialize the first level, but don't add it
            // to the list of the levels becuase it contains the initial state
            MinimaxLevel currentLevel = new MinimaxLevel(isMax, initialNode);

            // if the current level has nodes and the number of levels
            // didn't exceed the max, continue looping (move to the next level)
            while (levelCount < maxLevels)
            {
                // set the mini-max value and tile of the current player
                minimaxValue = isMax ? -1 : +1;
                nextLevelTile = isMax ? TileC : TileH;

                // choose the other player as the player of the new level
                isMax = !isMax;

                // loop on the nodes of the current level, get their children
                // get children only if the node does is not complete nor winning
                newLevelNodes = new List<MinimaxNode>();
                foreach (MinimaxNode node in currentLevel.Nodes)
                {
                    // check if the node is winning
                    if (IsWinning(node.State))
                    {
                        node.Value = minimaxValue;
                        continue;
                    }

                    // check if the node is complete
                    if (IsComplete(node.State))
                    {
                        //node.Value = 1;
                        node.Value = minimaxValue;
                        continue;
                    }

                    newLevelData = ChildrenStates(node.State, nextLevelTile);
                    node.Children = newLevelData.Select(i => new MinimaxNode(i, node)).ToArray();
                    newLevelNodes.AddRange(node.Children);

                    // increment nodes counter
                    nodeCount += newLevelData.Length;
                }

                // check if nodes were created for the new level
                if (newLevelNodes.Count > 0)
                {
                    // create the new level
                    currentLevel = new MinimaxLevel(isMax, newLevelNodes.ToArray());
                    levels.Add(currentLevel);

                    // increment levels counter
                    levelCount++;
                }
                else
                {
                    break;
                }
            }

            // for the last level, because the strategy applied here
            // is to stop searching when reaching certain max level, the last
            // level is likely to have un-finished nodes, set the value
            // of these nodes as if there are winning nodes
            MinimaxLevel lastLevel = levels[levels.Count - 1];
            minimaxValue = lastLevel.IsMax ? -1 : 1;
            foreach (MinimaxNode node in lastLevel.Nodes)
            {
                if (node.Value == 0)
                {
                    //node.Value = -1;
                    node.Value = minimaxValue;
                }
            }

            // now, we've all levels, bottom up, calculate all the minimax values
            // for the nodes in each level
            IEnumerable<int> values;
            for (int i = levels.Count - 2; i > -1; i--)
            {
                // for each node in the level, if it's value
                // was not set before (value still = 0), then set
                // its value as either min. or max. of the values of
                // the childs
                foreach (MinimaxNode node in levels[i].Nodes)
                {
                    if (node.Value == 0)
                    {
                        values = node.Children.Select(j => j.Value);
                        node.Value = levels[i].IsMax ? values.Max() : values.Min();
                    }
                }
            }

            // for the values of the nodes in the first level
            // get which one of these nodes has value = 1
            // then get the column that was played by the 'Computer'
            List<int> columns = new List<int>();
            int column;
            for (int i = 0; i < levels[0].Nodes.Length; i++)
            {
                if (levels[0].Nodes[i].Value == 1)
                {
                    columns.Add(UnlikeColumn(levels[0].Nodes[i].State, state));
                }
            }

            // check if there is no winning column
            if (columns.Count > 0)
            {
                column = GetRandomColum(columns);
                Utils.DebugLine("Wining Columns: " + string.Join(",", columns));
            }
            else
            {
                Utils.DebugLine("No 100% winning case, choosing random column!");

                // strategy 2
                // another strategy to recall the minimax with less max level
                //maxLevels--;
                //column = Minimax(state, maxLevels);
                //column = GetRandomColum(columns);

                // strategy 3
                // choose the column that will stop any comming winning step
                // by the other player 'Human'
                column = DefensiveColumn(state);
                if (column == -1)
                {
                    // strategy 1
                    // since there is no column with 100% winning ratio for 'Computer'
                    // nor defensive column was found, then choose arbitrary column that has space
                    // at this point, all culations say if 'Human' is smart, 'Computer'
                    // will never won this game that's why the computer is playing arbitrarely!
                    columns = new List<int>();
                    for (int y = 0; y < boardHeight; y++)
                    {
                        for (int x = 0; x < boardWidth; x++)
                        {
                            if (state[y][x] == TileE)
                            {
                                columns.Add(y);
                                break;
                            }
                        }
                    }

                    column = GetRandomColum(columns);
                }
            }

            PrintSearchTree(levels);
            return column;
        }

        /// <summary>
        /// Weather is the given state is considered a winning one or not.
        /// </summary>
        /// <returns></returns>
        private bool IsWinning(char[][] state)
        {
            int count1 = 0;
            int count2 = 0;

            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    #region 1. look for n connected tiles horizontally

                    // 1.1 right to the current
                    if (x + tilesToWin - 1 < boardWidth)
                    {
                        count1 = 0;
                        count2 = 0;
                        for (int i = x; i < x + tilesToWin; i++)
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

                        if (count1 == tilesToWin || count2 == tilesToWin)
                        {
                            return true;
                        }
                    }

                    // 1.2 left to the current
                    if (x - tilesToWin + 1 > -1)
                    {
                        count1 = 0;
                        count2 = 0;
                        for (int i = x; i > x - tilesToWin; i--)
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

                        if (count1 == tilesToWin || count2 == tilesToWin)
                        {
                            return true;
                        }
                    }

                    #endregion

                    #region 2. look for n connected tiles vertically

                    // 2.1 buttom of the current
                    if (y + tilesToWin - 1 < boardHeight)
                    {
                        count1 = 0;
                        count2 = 0;
                        for (int i = y; i < y + tilesToWin; i++)
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

                        if (count1 == tilesToWin || count2 == tilesToWin)
                        {
                            return true;
                        }
                    }

                    // 2.2 top of the current
                    if (y - tilesToWin + 1 > -1)
                    {
                        count1 = 0;
                        count2 = 0;
                        for (int i = y; i > y - tilesToWin; i--)
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

                        if (count1 == tilesToWin || count2 == tilesToWin)
                        {
                            return true;
                        }
                    }

                    #endregion

                    #region 3. look for n connected tiles diagonally

                    // 3.1 south-east
                    if (x + tilesToWin - 1 < boardWidth
                        && y + tilesToWin - 1 < boardHeight)
                    {
                        count1 = 0;
                        count2 = 0;
                        for (int i = 0; i < tilesToWin; i++)
                        {
                            if (state[y + i][x + i] == TileH)
                            {
                                count1++;
                            }
                            else if (state[y + i][x + i] == TileC)
                            {
                                count2++;
                            }
                        }

                        if (count1 == tilesToWin || count2 == tilesToWin)
                        {
                            return true;
                        }
                    }

                    // 3.2 north-east
                    if (x + tilesToWin - 1 < boardWidth
                        && y - tilesToWin + 1 > -1)
                    {
                        count1 = 0;
                        count2 = 0;
                        for (int i = 0; i < tilesToWin; i++)
                        {
                            if (state[y - i][x + i] == TileH)
                            {
                                count1++;
                            }
                            else if (state[y - i][x + i] == TileC)
                            {
                                count2++;
                            }
                        }

                        if (count1 == tilesToWin || count2 == tilesToWin)
                        {
                            return true;
                        }
                    }

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
            for (int x = 0; x < boardWidth; x++)
            {
                if (state[0][x] == TileE)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check if this is initial state (no player has played yet).
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool IsInitial(char[][] state)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    if (state[y][x] != TileE)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Get the possible children (next possible game states) for the current game state.
        /// </summary>
        private char[][][] ChildrenStates(char[][] parentState, char playerTile)
        {

            List<char[][]> children = new List<char[][]>();
            char[][] child = null;

            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = boardHeight - 1; y > -1; y--)
                {
                    if (parentState[y][x] == TileE)
                    {
                        child = CloneState(parentState);
                        child[y][x] = playerTile;
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
            char[][] newState = new char[boardHeight][];

            for (int y = 0; y < boardHeight; y++)
            {
                newState[y] = new char[boardWidth];
                for (int x = 0; x < boardWidth; x++)
                {
                    newState[y][x] = state[y][x];
                }
            }

            return newState;
        }

        /// <summary>
        /// Return the first column that is different in the given states.
        /// </summary>
        /// <returns></returns>
        private int UnlikeColumn(char[][] stateA, char[][] stateB)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    if (stateA[y][x] != stateB[y][x])
                    {
                        return x;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private int DefensiveColumn(char[][] state)
        {
            int countY = 0;
            int countX = 0;

            int yIndex;

            // loop on the columns of the game
            for (int x = 0; x < boardWidth; x++)
            {
                // check if the  column has empty tile (1 at least)
                // so this column can have a next step
                yIndex = -1;
                for (int y = boardHeight - 1; y > -1; y--)
                {
                    if (state[y][x] == TileE)
                    {
                        yIndex = y;
                        break;
                    }
                }
                if (yIndex == -1)
                {
                    continue;
                }

                // now we're sure that this column contains an empty tile
                // and we have the row number of this tile
                // go check the tiles under it, if they are TilesToWin-1
                // and all of them belong to the opponent 'Human', return
                // the current column
                if (boardHeight - (yIndex + 1) >= tilesToWin - 1)
                {
                    bool isOpporentTile = true;
                    for (int i = 1; i < tilesToWin; i++)
                    {
                        if (state[yIndex + i][x] != TileH)
                        {
                            isOpporentTile = false;
                            break;
                        }
                    }
                    if (isOpporentTile)
                    {
                        return x;
                    }
                }

                // now check the left and right tiles to see if the opponent
                // will win of he places his tile in the current column
                // in the current row (yIndex)
                // look on the right
                if (boardWidth - (x + 1) >= tilesToWin - 1)
                {
                    bool isOpporentTile = true;
                    for (int i = 1; i < tilesToWin; i++)
                    {
                        if (state[yIndex][x + i] != TileH)
                        {
                            isOpporentTile = false;
                            break;
                        }
                    }
                    if (isOpporentTile)
                    {
                        return x;
                    }
                }
                // look on the left
                if (x >= tilesToWin - 1)
                {
                    bool isOpporentTile = true;
                    for (int i = 1; i < tilesToWin; i++)
                    {
                        if (state[yIndex][x - i] != TileH)
                        {
                            isOpporentTile = false;
                            break;
                        }
                    }
                    if (isOpporentTile)
                    {
                        return x;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Get random column from the columns in the given array.
        /// </summary>
        /// <returns></returns>
        private int GetRandomColum(List<int> columns)
        {
            Random random = new Random();
            int index = random.Next(0, columns.Count);
            int column = columns[index];
            return column;
        }

        /// <summary>
        /// Return max number of levels to be searched.
        /// </summary>
        /// <returns></returns>
        private int MaxLevel()
        {
            int maxLevels;
            int tileCount = boardWidth * boardHeight;
            if (tileCount <= 12)
            {
                maxLevels = 10;
            }
            else if (tileCount <= 20)
            {
                maxLevels = 8;
            }
            else
            {
                maxLevels = 7;
            }

            return maxLevels;
        }

        /// <summary>
        /// Reset the game.
        /// </summary>
        private void ResetGame()
        {
            // reset the tiles
            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    blocks[y][x].Fill = transparentBrush;
                }
            }

            // define initial state
            boardState = new char[boardHeight][];
            for (int y = 0; y < boardHeight; y++)
            {
                boardState[y] = new char[boardWidth];
                for (int x = 0; x < boardWidth; x++)
                {
                    boardState[y][x] = TileE;
                }
            }

            // reset the counters
            nodeCount = 0;
            levelCount = 0;

            // game stats
            currentPlayer = (bool)radioButtonHuman.IsChecked ? PlayerType.Human : PlayerType.Computer;
            labelGameStatus.Content = gameStatus.Description();
            labelCurrentPlayer.Content = currentPlayer.Description();
            ellipseCurrentPlayer.Fill = PlayerColor(currentPlayer);
            textBoxOutput.Text = string.Empty;
        }

        /// <summary>
        /// Display a message declaring the end of the game and the result.
        /// </summary>
        private void FinishGame(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ButtonStart_Click(null, null);
                groupBoxGameBoard.IsEnabled = false;
                MessageBox.Show(message, "Game", MessageBoxButton.OK);
            });
        }

        #endregion
    }
}