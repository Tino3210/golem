using System;
using System.Collections.Generic;
using System.Linq;

namespace OthelloFrossardIzzo
{    
    // Tile states
    public enum TileState
    {
        EMPTY = -1,
        WHITE = 0,
        BLACK = 1
    }

    public class Node
    {
        const int BOARDSIZE_X = 9;
        const int BOARDSIZE_Y = 7;

        double[,] valueCell = new double[,] {{16.16 ,-3.03  ,1.33   ,-2.67  ,2.67   ,-2.67  ,1.33   ,-3.03  ,16.16} ,
                                            {-4.12  ,-1.81  ,-0.9   ,-0.9   ,-1.16  ,-0.9   ,-0.9   ,-1.81  ,-4.12} ,
                                            {1.33   ,-0.9   ,0      ,0      ,0      ,0      ,0      ,-0.9   ,1.33}  ,
                                            {-2.67  ,1.2    ,0      ,-1     ,1      ,0      ,0      ,1.2    ,-2.67} ,
                                            {1.33   ,-0.9   ,0      ,1      ,-1     ,0      ,0      ,-0.9   ,1.33}  ,
                                            {-4.12  ,-1.81  ,-0.9   ,-0.9   ,-1.16  ,-0.9   ,-0.9   ,-1.81  ,-4.12} ,
                                            {16.16  ,-3.03  ,1.33   ,-2.67  ,2.67   ,-2.67  ,1.33   ,-3.03  ,16.16 } };

        int whiteScore = 0;
        int blackScore = 0;

        public int flipTiles;

        public Node(int[,] board, bool whiteTurn)
        {
            this.parent = null;
            this.ops = new List<Node>();
            this.currentBoard = board;
            this._whiteTurn = whiteTurn;
            this.possibleMove = null;
        }

        public bool GameFinish { get; set; }

        public Node parent { get; set; }
        public bool _whiteTurn { get; set; }
        public int[,] currentBoard { get; set; }
        public Tuple<int,int> possibleMove { get; set; }
        public List<Node> ops;


        public Node apply(Tuple<int,int> move)
        {
            //Console.WriteLine("Possible Move : " + move);
            Node newChildren = null;
            newChildren = new Node(currentBoard.Clone() as int[,], !_whiteTurn);
            /*if (parent != null)
            {
                newChildren = new Node(currentBoard.Clone() as int[,], !whiteTurn);
            }
            else
            {
                newChildren = new Node(currentBoard.Clone() as int[,], whiteTurn);
            }*/
            newChildren.parent = this;
            newChildren.possibleMove = move;
            newChildren.PlayMove(move.Item1, move.Item2, _whiteTurn);
            
            ops.Add(newChildren);
            //Console.WriteLine("Children (" + move.Item1 + ", " + move.Item2 + ")");
            //newChildren.displayBoard();
            return newChildren;
        }

        public double Eval()
        {
            double value = valueCell[possibleMove.Item2, possibleMove.Item1];

            if (CanBlockEnemy())
            {
                value = 15;
            }


            /*
            * Si adversaire a plus de pièce
            */
            if (_whiteTurn && _whiteTurn != MyColor())
            {
                if (whiteScore == 0)
                {
                    value = 16;
                }
            }
            if (!_whiteTurn && !_whiteTurn != MyColor())
            {
                if (blackScore == 0)
                {
                    value = 16;
                }
            }

            return value;
        }

        public bool CanBlockEnemy()
        {
            if (MyColor() == _whiteTurn)
            {
                return false;
            }
            return GetPossibleMove().Count() == 0;
        }

        public bool MyColor()
        {
            Node node = this;
            while (node.parent != null)
            {
                node = node.parent;
            }
            return node._whiteTurn;
        }

        private void computeScore()
        {
            whiteScore = 0;
            blackScore = 0;
            foreach (var v in currentBoard)
            {
                if (v == (int)TileState.WHITE)
                    whiteScore++;
                else if (v == (int)TileState.BLACK)
                    blackScore++;
            }
            GameFinish = ((whiteScore == 0) || (blackScore == 0) ||
                        (whiteScore + blackScore == 63));
        }

        public bool PlayMove(int column, int line, bool whiteTurn)
        {
            //0. Verify if indices are valid
            if ((column < 0) || (column >= BOARDSIZE_X) || (line < 0) || (line >= BOARDSIZE_Y))
                return false;
            //1. Verify if it is playable
            if (IsPlayable(column, line, whiteTurn) == false)
                return false;

            //2. Create a list of directions {dx,dy,length} where tiles are flipped
            int c = column, l = line;
            bool playable = false;
            TileState opponent = whiteTurn ? TileState.BLACK : TileState.WHITE;
            TileState ownColor = (!whiteTurn) ? TileState.BLACK : TileState.WHITE;
            List<Tuple<int, int, int>> catchDirections = new List<Tuple<int, int, int>>();

            for (int dLine = -1; dLine <= 1; dLine++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    c = column + dCol;
                    l = line + dLine;
                    if ((c < BOARDSIZE_X) && (c >= 0) && (l < BOARDSIZE_Y) && (l >= 0)
                        && (currentBoard[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        int counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0))
                                   && (currentBoard[c, l] == (int)opponent)) // pour éviter les trous
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (currentBoard[c, l] == (int)ownColor)
                            {
                                playable = true;
                                currentBoard[column, line] = (int)ownColor;
                                catchDirections.Add(new Tuple<int, int, int>(dCol, dLine, counter));
                            }
                        }
                    }
                }
            }
            // 3. Flip ennemy tiles
            foreach (var v in catchDirections)
            {
                int counter = 0;
                l = line;
                c = column;
                while (counter++ < v.Item3)
                {
                    c += v.Item1;
                    l += v.Item2;
                    currentBoard[c, l] = (int)ownColor;
                }
            }
            //Console.WriteLine("CATCH DIRECTIONS:" + catchDirections.Count);
            computeScore();
            return playable;
        }

        public List<Tuple<int, int>> GetPossibleMove()
        {
            char[] colonnes = "ABCDEFGHIJKL".ToCharArray();
            List<Tuple<int, int>> possibleMoves = new List<Tuple<int, int>>();
            for (int i = 0; i < BOARDSIZE_X; i++)
                for (int j = 0; j < BOARDSIZE_Y; j++)
                {
                    if (IsPlayable(i, j, _whiteTurn))
                    {
                        possibleMoves.Add(new Tuple<int, int>(i, j));
                        //Console.WriteLine("Possible : " + i + ", " + j + " Turn :" + _whiteTurn);
                    }
                }
            return possibleMoves;
        }

        public bool IsPlayable(int column, int line, bool isWhite)
        {
            //1. Verify if the tile is empty !
            if (currentBoard[column, line] != (int)TileState.EMPTY)
                return false;
            //2. Verify if at least one adjacent tile has an opponent tile
            TileState opponent = isWhite ? TileState.BLACK : TileState.WHITE;
            TileState ownColor = (!isWhite) ? TileState.BLACK : TileState.WHITE;
            int c = column, l = line;
            bool playable = false;
            List<Tuple<int, int, int>> catchDirections = new List<Tuple<int, int, int>>();
            for (int dLine = -1; dLine <= 1; dLine++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    c = column + dCol;
                    l = line + dLine;
                    if ((c < BOARDSIZE_X) && (c >= 0) && (l < BOARDSIZE_Y) && (l >= 0)
                        && (currentBoard[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        int counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0)))
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (currentBoard[c, l] == (int)ownColor)
                            {
                                playable = true;
                                break;
                            }
                            else if (currentBoard[c, l] == (int)opponent)
                                continue;
                            else if (currentBoard[c, l] == (int)TileState.EMPTY)
                                break;  //empty slot ends the search
                        }
                    }
                }
            }
            return playable;
        }

        public void displayBoard(int[,] board = null)
        {
            Console.WriteLine("Whiteturn : " + this._whiteTurn);
            if (board == null)
            {
                board = currentBoard;
            }
            for(int line = 0; line < BOARDSIZE_Y; line++)
            {
                for(int col = 0; col < BOARDSIZE_X; col++)
                {                    
                    switch (board[col,line])
                    {
                        case -1:
                            Console.Write("| " + '*' /*+ " : " + valueCell[line,col]*/);
                            break;
                        case 1:
                            Console.Write("| " + 'B' /*+ " : " + valueCell[line, col]*/);
                            break;
                        case 0:
                            Console.Write("| " + 'W' /*+ " : " + valueCell[line, col]*/);
                            break;
                    }                   
                }
                Console.WriteLine("|");
            }
            Console.WriteLine("\n");
        }
    }

    public class OthelloBoard : IPlayable.IPlayable
    {
        const int BOARDSIZE_X = 9;
        const int BOARDSIZE_Y = 7;

        int[,] theBoard = new int[BOARDSIZE_X, BOARDSIZE_Y];
        int whiteScore = 0;
        int blackScore = 0;
        public bool GameFinish { get; set; }

        private Random rnd = new Random();

        public OthelloBoard()
        {
            initBoard();
        }

        /// <summary>
        /// Returns the board game as a 2D array of int
        /// with following values
        /// -1: empty
        ///  0: white
        ///  1: black
        /// </summary>
        /// <returns></returns>
        public int[,] GetBoard()
        {
            return (int[,])theBoard;
        }


        #region IPlayable
        public int GetWhiteScore() { return whiteScore; }
        public int GetBlackScore() { return blackScore; }
        public string GetName() { return "Le Golem"; }

        /// <summary>
        /// This function is called by the controller to get the next move you want to play
        /// </summary>
        /// <param name="game"></param>
        /// <param name="level"></param>
        /// <param name="whiteTurn"></param>
        /// <returns>The move it will play, will return {-1,-1} if it has to PASS its turn (no move is possible)</returns>
        public Tuple<int, int> GetNextMove(int[,] game, int level, bool whiteTurn)
        {
            List<Tuple<int, int>> possibleMoves = GetPossibleMove(whiteTurn);
            if (possibleMoves.Count == 0)
            {
                return new Tuple<int, int>(-1, -1);
            }
            else
            {
                Node rootNode = new Node(theBoard.Clone() as int[,], whiteTurn);
                Tuple<double, Node>  result = alphabeta(rootNode, 5, 1, double.PositiveInfinity);
                Console.WriteLine("Score " + result.Item1);
                if (result.Item2 == null)
                {
                    Console.WriteLine("passe le tour");
                    return new Tuple<int, int>(-1, -1);
                }
                return result.Item2.possibleMove;
            }
        }

        /*
         * -------------------------------------------------
         *
         */

        public Tuple<double, Node> alphabeta(Node root, int depth, double minOrMax, double parentValue)
        {
            //Console.WriteLine("\nDEPTH : " + depth);
            if(depth == 0 || root.GameFinish)
            {
                return new Tuple<double, Node>(root.Eval(), null);
            }
            double optVal = minOrMax * double.NegativeInfinity;
            Node optOp = null;

            //Console.WriteLine("Daron");
            //root.displayBoard();
            foreach (var op in root.GetPossibleMove())
            {
                Node newNode = root.apply(op);
                Tuple<double, Node> valDummy = alphabeta(newNode, depth - 1, -minOrMax, optVal);
                if(valDummy.Item1 * minOrMax > optVal * minOrMax)
                {                    
                    optVal = valDummy.Item1;
                    optOp = newNode;

                    if(optVal * minOrMax > parentValue * minOrMax)              
                        break;
                }
            }
            return new Tuple<double, Node>(optVal, optOp);
        }

        /*
         * -------------------------------------------------
         *
         */

        /// <summary>
        /// This function is never called by the controller. 
        /// It is here to help you play on your side
        /// </summary>
        /// <param name="column"></param>
        /// <param name="line"></param>
        /// <param name="isWhite"></param>
        /// <returns></returns>
        public bool PlayMove(int column, int line, bool isWhite)
        {
            //0. Verify if indices are valid
            if ((column < 0) || (column >= BOARDSIZE_X) || (line < 0) || (line >= BOARDSIZE_Y))
                return false;
            //1. Verify if it is playable
            if (IsPlayable(column, line, isWhite) == false)
                return false;
            
            //2. Create a list of directions {dx,dy,length} where tiles are flipped
            int c = column, l = line;
            bool playable = false;
            TileState opponent = isWhite ? TileState.BLACK : TileState.WHITE;
            TileState ownColor = (!isWhite) ? TileState.BLACK : TileState.WHITE;
            List<Tuple<int, int, int>> catchDirections = new List<Tuple<int, int, int>>();

            for (int dLine = -1; dLine <= 1; dLine++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    c = column + dCol;
                    l = line + dLine;
                    if ((c < BOARDSIZE_X) && (c >= 0) && (l < BOARDSIZE_Y) && (l >= 0)
                        && (theBoard[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        int counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0))
                                   && (theBoard[c, l] == (int)opponent)) // pour éviter les trous
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (theBoard[c, l] == (int)ownColor)
                            {
                                playable = true;
                                theBoard[column, line] = (int)ownColor;
                                catchDirections.Add(new Tuple<int, int, int>(dCol, dLine, counter));
                            }
                        }
                    }
                }
            }
            // 3. Flip ennemy tiles
            foreach (var v in catchDirections)
            {
                int counter = 0;
                l = line;
                c = column;
                while (counter++ < v.Item3)
                {
                    c += v.Item1;
                    l += v.Item2;
                    theBoard[c, l] = (int)ownColor;
                }
            }
            //Console.WriteLine("CATCH DIRECTIONS:" + catchDirections.Count);
            computeScore();
            return playable;
        }

        /// <summary>
        /// More convenient overload to verify if a move is possible
        /// </summary>
        /// <param name=""></param>
        /// <param name="isWhite"></param>
        /// <returns></returns>
        public bool IsPlayable(Tuple<int, int> move, bool isWhite)
        {
            return IsPlayable(move.Item1, move.Item2, isWhite);
        }

        public bool IsPlayable(int column, int line, bool isWhite)
        {
            //1. Verify if the tile is empty !
            if (theBoard[column, line] != (int)TileState.EMPTY)
                return false;
            //2. Verify if at least one adjacent tile has an opponent tile
            TileState opponent = isWhite ? TileState.BLACK : TileState.WHITE;
            TileState ownColor = (!isWhite) ? TileState.BLACK : TileState.WHITE;
            int c = column, l = line;
            bool playable = false;
            List<Tuple<int, int, int>> catchDirections = new List<Tuple<int, int, int>>();
            for (int dLine = -1; dLine <= 1; dLine++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    c = column + dCol;
                    l = line + dLine;
                    if ((c < BOARDSIZE_X) && (c >= 0) && (l < BOARDSIZE_Y) && (l >= 0)
                        && (theBoard[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        int counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0)))
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (theBoard[c, l] == (int)ownColor)
                            {
                                playable = true;
                                break;
                            }
                            else if (theBoard[c, l] == (int)opponent)
                                continue;
                            else if (theBoard[c, l] == (int)TileState.EMPTY)
                                break;  //empty slot ends the search
                        }
                    }
                }
            }
            return playable;
        }
        #endregion

        /// <summary>
        /// Returns all the playable moves in a computer readable way (e.g. "<3, 0>")
        /// </summary>
        /// <param name="v"></param>
        /// <param name="whiteTurn"></param>
        /// <returns></returns>
        public List<Tuple<int, int>> GetPossibleMove(bool whiteTurn, bool show = false)
        {
            char[] colonnes = "ABCDEFGHIJKL".ToCharArray();
            List<Tuple<int, int>> possibleMoves = new List<Tuple<int, int>>();
            for (int i = 0; i < BOARDSIZE_X; i++)
                for (int j = 0; j < BOARDSIZE_Y; j++)
                {
                    if (IsPlayable(i, j, whiteTurn))
                    {
                        possibleMoves.Add(new Tuple<int, int>(i, j));
                        //Uncomment if you want to print the possibles moves
                        if (show == true)
                            Console.Write((colonnes[i]).ToString() + (j + 1).ToString() + ", ");
                    }
                }
            return possibleMoves;
        }

        /// <summary>
        /// Init the board for a new game and save it in the class variable : "theboard"
        /// </summary>
        private void initBoard()
        {
            for (int i = 0; i < BOARDSIZE_X; i++)
                for (int j = 0; j < BOARDSIZE_Y; j++)
                    theBoard[i, j] = (int)TileState.EMPTY;

            theBoard[3, 3] = (int)TileState.WHITE;
            theBoard[4, 4] = (int)TileState.WHITE;
            theBoard[3, 4] = (int)TileState.BLACK;
            theBoard[4, 3] = (int)TileState.BLACK;

            computeScore();
        }

        /// <summary>
        /// Calculate the score, the number of white pawn and black pawn
        /// </summary>
        private void computeScore()
        {
            whiteScore = 0;
            blackScore = 0;
            foreach (var v in theBoard)
            {
                if (v == (int)TileState.WHITE)
                    whiteScore++;
                else if (v == (int)TileState.BLACK)
                    blackScore++;
            }
            GameFinish = ((whiteScore == 0) || (blackScore == 0) ||
                        (whiteScore + blackScore == 63));
        }
    }


}
