using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM.Cards;
using Crestron.SimplSharpPro.Fusion;
using Crestron.SimplSharpPro.Media;
using Crestron.SimplSharpPro.UI;
using Crestron.SimplSharpProInternal;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static Crestron.SimplSharpPro.Keypads.C2nLcdBXXBaseClass;

namespace TicTacToe
{
    public class Game
    {
        public int height;
        public int width;
        public int boardSize;
        public int emptySpaces;
        public string[] occupants;
        public int difficulty = 1;
        public Player playerX = new Player("X");
        public Player playerO = new Player("O");
        public Player nextPlayer;
        public int tieGames = 0;

        public BasicTriListWithSmartObject panel;


        // The board accepts a height and width. Currently these are simply multiplied
        // to form a length of the array. Eventually, they may be used to generate different
        // sized boards and develop rulesets accordingly.
        public Game(int Height, int Width)
        {
            height = Height;
            width = Width;
            boardSize = height * width;
            occupants = new string[boardSize];

        }

        public void NewGame()
        {
            emptySpaces = boardSize;
            panel.StringInput[1].StringValue = "";

            for (uint i = 0; i < occupants.Length; i++)
            {
                occupants[i] = " ";
                panel.StringInput[i + 11].StringValue = " ";
            }
            if (nextPlayer == playerO)
            {
                uint oMove = Move(playerO, playerX);
                occupants[oMove] = "O";
                panel.StringInput[oMove + 11].StringValue = "O";
            }
        }

        public void ResetAll()
        {
            tieGames = 0;
            playerX.wins = 0;
            playerO.wins = 0;
            nextPlayer = playerX;
            for (uint i = 21; i < 24; i++) panel.StringInput[i].StringValue = "0";
            for (uint i = 31; i < 34; i++) panel.BooleanInput[i].BoolValue = (i == difficulty + 30);

            NewGame();
            panel.StringInput[1].StringValue = "Welcome to Tic Tac Toe! You go first!";

        }


        // This method checks the validity of a player move, makes the move if valid,
        // and then responds appropriately, either by declaring the result of the game
        // or making a move for the AI player
        public void PlayerMove(int spaceNum)
        {
            // Check to see if the game is already over. If so, prompt the player to start new game.
            if (CheckResult() != null)
            {
                panel.StringInput[1].StringValue = "Please start a new game.";
            }

            // Check to see if selected spot is empty; report occupant if it is not
            else if (occupants[spaceNum] != " ")
            {
                panel.StringInput[1].StringValue = $"That space is already taken by {occupants[spaceNum]}!";
            }

            // If space is not empty, make a move for X
            else
            {
                occupants[spaceNum] = playerX.letter;
                emptySpaces--;
                panel.StringInput[(uint)spaceNum + 11].StringValue = occupants[spaceNum];
                nextPlayer = playerO;


                // Check to see if X has won the game

                if (CheckResult() == "X")
                {
                    playerX.wins++;
                    panel.StringInput[21].StringValue = playerX.wins.ToString();
                    panel.StringInput[1].StringValue = "You win!";
                    Comment("X Wins!");
                    CommentBoard();
                }

                // If X has not won the game, see if there are any empty spaces left
                else if (emptySpaces > 0)
                {
                    // If there are any empty spaces, O moves, selecting its move based on
                    // selected difficulty

                    uint oMove = Move(playerO, playerX);
                    occupants[oMove] = "O";
                    emptySpaces--;
                    panel.StringInput[oMove + 11].StringValue = "O";
                    nextPlayer = playerX;

                    // Check to see if O's move won the game
                    if (CheckResult() == "O")
                    {
                        playerO.wins++;
                        panel.StringInput[22].StringValue = playerO.wins.ToString();
                        panel.StringInput[1].StringValue = "Crestron Wins!";
                        Comment("O Wins!");
                        CommentBoard();
                    }
                    else if (emptySpaces == 0)
                    {
                        tieGames++;
                        panel.StringInput[23].StringValue = tieGames.ToString();
                        Comment("Tie game!");
                        CommentBoard();
                    }

                }

                // If there aren't any empty spaces and neither player has won,
                // declare the game a tie.

                else
                {
                    tieGames++;
                    panel.StringInput[23].StringValue = tieGames.ToString();
                    panel.StringInput[1].StringValue = "It's a tie!";
                    Comment("Tie game!");
                    CommentBoard();

                }


            }
        }

        // A utility that writes comments on move selections (or anything else)
        // to the text file created on the device
        public void Comment(string text)
        {
            string fileDir = Directory.GetApplicationRootDirectory();
            string filePath = Path.Combine(fileDir, "tictactoe.txt");
            FileStream fs = new FileStream($"{filePath}", FileMode.Append);
            using (StreamWriter sw = new StreamWriter(fs)) sw.WriteLine(text);
        }


        // Prints a basic representation of the board to file. Useful for 
        // debugging AI move selections and conditions
        public void CommentBoard()

        {
            string fileDir = Directory.GetApplicationRootDirectory();
            string filePath = Path.Combine(fileDir, "tictactoe.txt");
            FileStream fs = new FileStream($"{filePath}", FileMode.Append);
            using (StreamWriter sw = new StreamWriter(fs)) sw.WriteLine($"{occupants[0]}|{occupants[1]}|{occupants[2]}\n" +
                                                                        $"-+-+-\n" +
                                                                        $"{occupants[3]}|{occupants[4]}|{occupants[5]}\n" +
                                                                        $"-+-+-\n" +
                                                                        $"{occupants[6]}|{occupants[7]}|{occupants[8]}\n" +
                                                                        $"");

        }

        public void SetDifficulty(int diff)
        {
            difficulty = diff;
            for (uint i = 31; i < 34; i++) panel.BooleanInput[i].BoolValue = (i == diff + 30);

        }


        // A simple method to compare three values
        public bool Threequal(object value1, object value2, object value3)
        {
            if (value1 == value2 && value2 == value3)
            {
                return true;
            }
            return false;

        }

        // This method returns the result of the game by checking all lines on the board and seeing if
        // they are (1) all the same and (2) not empty. If so, the value of the matching spaces is
        // returned. If the board is full, but neither player has won, the game is declared a tie.
        // Otherwise, the method returns a null value.

        public string CheckResult()
        {
            if (Threequal(occupants[0], occupants[1], occupants[2]) && occupants[0] != " ") return occupants[0];
            else if (Threequal(occupants[3], occupants[4], occupants[5]) && occupants[3] != " ") return occupants[3];
            else if (Threequal(occupants[6], occupants[7], occupants[8]) && occupants[6] != " ") return occupants[6];
            else if (Threequal(occupants[0], occupants[3], occupants[6]) && occupants[0] != " ") return occupants[0];
            else if (Threequal(occupants[1], occupants[4], occupants[7]) && occupants[1] != " ") return occupants[1];
            else if (Threequal(occupants[2], occupants[5], occupants[8]) && occupants[2] != " ") return occupants[2];
            else if (Threequal(occupants[0], occupants[4], occupants[8]) && occupants[0] != " ") return occupants[0];
            else if (Threequal(occupants[2], occupants[4], occupants[6]) && occupants[2] != " ") return occupants[2];
            else if (!occupants.Contains(" ")) return "Tie";
            else return null;


        }

        // Method for returning the optimal move for the AI player. The opponent parameter is not necessary for
        // the AI to play at maximal difficulty, but for the more simplistic Medium difficulty, it is used to
        // check squares for a potential win by the human player
        public uint Move(Player player, Player opponent)
        {

            int bestScore = -100;
            int score;

            // To save computing time, the first empty square is found before beginning to check each square
            // as a potential move.
            int firstEmptySpace = Array.IndexOf(occupants, " ");
            uint bestMove = (uint)firstEmptySpace;


            // On Easy difficulty, the AI simply finds a random open space and plays there.
            if (difficulty == 1)
            {
                Random random = new Random();
                bestMove = (uint)random.Next(occupants.Length);
                while (occupants[(int)bestMove] != " ") bestMove = (uint)random.Next(occupants.Length);
            }

            // On Medium and Hard difficulty, the AI looks to see if it has an immediate win (which it takes)
            // or if the human player has an immediate thread to win (which it blocks). This is technically
            // unnecessary for the Hard difficulty's algorithm to work, but it does avoid a quirk where the
            // AI may not automatically take an immediate win, but will instead choose a different spot
            // that also guarantees a win, giving the appearance of "toying" with its human prey.
            else
            {

                for (int i = firstEmptySpace; i < occupants.Length; i++)
                {
                    if (occupants[i] == " ")
                    {
                        occupants[i] = player.letter;
                        if (CheckResult() == player.letter) return (uint)i;
                        occupants[i] = " ";
                    }
                }
                for (int i = firstEmptySpace; i < occupants.Length; i++)
                {
                    if (occupants[i] == " ")
                    {
                        occupants[i] = opponent.letter;
                        if (CheckResult() == opponent.letter) return (uint)i;
                        occupants[i] = " ";
                    }
                }

                if (difficulty == 2)
                {
                    Random random = new Random();
                    bestMove = (uint)random.Next(occupants.Length);
                    while (occupants[(int)bestMove] != " ") bestMove = (uint)random.Next(occupants.Length);
                }

                // The Hard difficulty checks to see which spaces are available, and for each available
                // space, uses a variation on a MiniMax algorithm to calculate a score. The highest
                // scoring space in the AI's estimation is selected.

                else
                {
                    for (int i = firstEmptySpace; i < occupants.Length; i++)
                    {
                        if (occupants[i] == " ")
                        {
                            occupants[i] = player.letter;
                            score = MiniMax(-1);
                            occupants[i] = " ";
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestMove = (uint)i;
                            }

                        }
                    }
                }

            }

            return bestMove;

        }

        // The MiniMax algorithm considers all possible squares and checks for a terminal state (a win
        // or a full board). A win by either player generates a value of -1 for X or 1 for O. Tie
        // generates a score of 0 points. These values are stored in a dictionary for comparing against
        // space values and generating integer outcomes that can later be utilized. The goal parameter
        // passed into the method is used determine whether the player whose move is being simulated
        // wants the AI to win or the player to win.
        public int MiniMax(int goal)
        {
            var outcomes = new Dictionary<string, int>
                {
                    { "X", -1 },
                    { "Tie", 0 },
                    { "O", 1 }
                };
            int bestScore;
            int score;
            int firstEmptySpace = Array.IndexOf(occupants, " ");

            // If a terminal state (win or tie) is reached by the move, that move generates a score
            if (CheckResult() != null)
            {
                return outcomes[CheckResult()];
            }

            // If no terminal state is found, the Minimax is recursively called to figure out the next
            // player's optimal move. Notice that the goal parameter is multiplied by -1 in these recursive
            // calls. This is used not only to check the score of each potential move, but also to 
            // perform a reverse search in the outcomes dictionary to choose which letter to place in
            // each space being tried. The use of this -1 minimizes the amount of repeated code, by 
            // implementing a signle, one-line conditional statement to determine whether to return the
            // highest or lowest score as "best"

            //this code works
            bestScore = -100 * goal;
            for (int i = firstEmptySpace; i < occupants.Length; i++)
            {
                if (occupants[i] == " ")
                {
                    occupants[i] = outcomes.FirstOrDefault(kvp => kvp.Value == goal).Key;
                    score = MiniMax(-1 * goal);
                    occupants[i] = " ";
                    bestScore = (goal == 1) ? Math.Max(score, bestScore) : Math.Min(score, bestScore);
                }
            }




            return bestScore;

        }


    }



}
