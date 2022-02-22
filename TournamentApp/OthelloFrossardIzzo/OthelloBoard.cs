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

        private int _whiteScore = 0;
        private int _blackScore = 0;
        private int _myState;
        private int _enemiState;
        private bool _myTurn;

        public bool GameFinish { get; set; }4

        private Node Parent { get; set; }
        private int MyNumberPossibleMove { get; set; }
        private bool WhiteTurn { get; set; }
        private int[,] CurrentBoard { get; set; }
        public Tuple<int, int> PossibleMove { get; set; }
        private List<Node> Ops;

        public Node(int[,] board, bool whiteTurn, int myNumberPossibleMove)
        {
            Parent = null;
            Ops = new List<Node>();
            CurrentBoard = board;
            WhiteTurn = whiteTurn;
            PossibleMove = null;
            MyNumberPossibleMove = myNumberPossibleMove;

            _myTurn = MyColor();
            _myState = _myTurn ? 1 : 0;
            _enemiState = _myState == 1 ? 0 : 1;
        }


        public Node apply(Tuple<int,int> move, int myNumberPossibleMove)
        {
            //Console.WriteLine("Possible Move : " + move);
            Node newChildren = null;
            newChildren = new Node(CurrentBoard.Clone() as int[,], !WhiteTurn, myNumberPossibleMove);
            newChildren.Parent = this;
            newChildren.PossibleMove = move;
            newChildren.PlayMove(move.Item1, move.Item2, WhiteTurn);
            
            Ops.Add(newChildren);
            //Console.WriteLine("Children (" + move.Item1 + ", " + move.Item2 + ")");
            //newChildren.displayBoard();
            return newChildren;
        }

        public double Eval()
        {
            double value = valueCell[PossibleMove.Item2, PossibleMove.Item1];

            if (CanBlockEnemy())
            {
                value += 50;
            }

            value += 1000 * NumberOfCorners();

            value += 11 * Frontier();
            value += 8 * CalculateMobility();


            if (_blackScore + _whiteScore > 30)
            {
                value += 5 * StableDisk();
                value += CalculateScore();
            }
            else
            {
                value += 2*PiecesToKeep();
            }

            value += 1000 * ParityScore();
            return value;
        }

        /// <summary>
        /// Calculate the percentage of the board
        /// </summary>
        /// <returns>a number [0,1]</returns>
        public double Ratio()
        {
            return (_whiteScore + _blackScore) / 63;
        }

        /// <summary>
        /// Check if the enemy can move
        /// </summary>
        /// <returns>true is the enemy cannot move</returns>
        public bool CanBlockEnemy()
        {
            if (MyColor() == WhiteTurn)
            {
                return false;
            }
            return GetPossibleMove().Count() == 0;
        }

        /// <summary>
        /// Give a heuristic in relation to the number of Frontier disk and Interior disk i own
        /// Frontier disk : is a disk which is near an empty space
        /// Interior disk : is a disk which has no near empty space
        /// </summary>
        /// <returns>The number of interior disk minus the number frontier disk</returns>
        public int Frontier()
        {
            int myCounterInterior = 0;
            int myCounterFrontiere = 0;

            //int enemieCounterInterior = 0;
            //int enemieCounterFrontiere = 0;

            for (int i = 0; i < BOARDSIZE_X - 1; i++)
            {
                for (int j = 0; j < BOARDSIZE_Y - 1; j++)
                {
                    if (IsRounded(i, j))
                    {
                        if (CurrentBoard[i, j] == _myState)
                        {
                            myCounterInterior += 1;
                        }
                        //else if (CurrentBoard[i, j] == _enemiState)
                        //{
                        //    enemieCounterInterior += 1;
                        //}
                    }
                    else
                    {
                        if (CurrentBoard[i, j] == _myState)
                        {
                            myCounterFrontiere += 1;
                        }
                        //else if (CurrentBoard[i, j] == _enemiState)
                        //{
                        //    enemieCounterFrontiere += 1;
                        //}
                    }

                }
            }
            return (myCounterInterior - myCounterFrontiere);

        }

        /// <summary>
        /// Check if a disk is surrounded by other disk
        /// </summary>
        /// <param name="x">the x axis</param>
        /// <param name="y">the y axis</param>
        /// <returns>true if it's surrounded</returns>
        public bool IsRounded(int x, int y)
        {
            int counter = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if ((x+i >= 0) && (y + j >= 0) && (x + i < BOARDSIZE_X) && (y + j < BOARDSIZE_Y))
                    {
                        if (CurrentBoard[x + i, y + j] == -1)
                        {
                            counter += 1;
                        }
                    }
                }
            }
            return (counter == 0) ? true : false;
        }

        /// <summary>
        /// Give a heuristic in relation to the number of stable disk i own
        /// </summary>
        /// <returns>The number i own minus the number enemy own</returns>
        public int StableDisk()
        {
            int counter = 0;

            for (int i = 0; i < BOARDSIZE_X- 1; i++)
            {
                for (int j = 0; j < BOARDSIZE_Y - 1; j++)
                {
                    if (CurrentBoard[i, j] != -1)
                    {
                        if (IsDefinitif(i, j))
                        {
                            if (CurrentBoard[i, j] == _myState)
                            {
                                counter += 1;
                            }
                            else if (CurrentBoard[i, j] == _enemiState)
                            {
                                counter -= 1;
                            }
                        }
                    }
                    
                }
            }

            return counter;
        }

        /// <summary>
        /// Check 4 directions(horizontal, vertical, both diagonal) if is full
        /// </summary>
        /// <param name="x">the x axis</param>
        /// <param name="y">the y axis</param>
        /// <returns>return true if the pieces cannot be flip</returns>
        public bool IsDefinitif(int x, int y)
        {
            for (int i = -x; i < BOARDSIZE_X - x-1; i++)
            {
                if (CurrentBoard[x+i, y] == -1)
                {
                    return false;
                }
            }

            for (int j = -y; j < BOARDSIZE_Y - y-1; j++)
            {
                if (CurrentBoard[x, y + j] == -1)
                {
                    return false;
                }
            }

            for (int i = -x; i < BOARDSIZE_X-x-1; i++)
            {
                for (int j = -y; j < BOARDSIZE_Y-y-1; j++)
                {
                    if (CurrentBoard[x+i, y+j] == -1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Give a heuristic in relation to the piece to keep in early game
        /// </summary>
        /// <returns>My number of "pieceToKeep" taken minus my opponent</returns>
        public int PiecesToKeep()
        {
            var piecesToKeep = new (int, int)[] {  (3,3) ,
                                                    (4,4) ,
                                                    (5,3) };

            int counter = 0;

            for (int i = 0; i < piecesToKeep.Length; i++)
            {
                if (CurrentBoard[piecesToKeep[i].Item1, piecesToKeep[i].Item2] == _myState)
                {
                    counter += 1;
                }
                else if (CurrentBoard[piecesToKeep[i].Item1, piecesToKeep[i].Item2] == _enemiState)
                {
                    counter -= 1;
                }
            }

            return counter;

        }

        /// <summary>
        /// Give a heuristic in relation to the number of corner I own
        /// </summary>
        /// <returns>My number of corner taken minus my opponent</returns>
        public int NumberOfCorners()
        {
            int numberCorners = 0;
            var positionCorners = new(int,int)[] {  (0,0) ,
                                                    (0,6) ,
                                                    (8,0) ,
                                                    (8,6) };

            for(int i = 0; i < positionCorners.Length; i++)
            {
                if (CurrentBoard[positionCorners[i].Item1, positionCorners[i].Item2] == _myState)
                {
                    numberCorners += 1;
                }
                else if(CurrentBoard[positionCorners[i].Item1, positionCorners[i].Item2] == _enemiState)
                {
                    numberCorners -= 1;
                }
            }

            return numberCorners;

        }
        
        /// <summary>
        /// Give a heuristic if I place the last piece on the board
        /// </summary>
        /// <returns>true if i finish else false</returns>
        public int ParityScore()
        {
            return (_blackScore + _whiteScore) % 2;
        }

        /// <summary>
        /// Give a heuristic with the score of the game
        /// </summary>
        /// <returns>Max int if i win and min int if i lose or my score minus the enemy score</returns>
        public int CalculateScore()
        {
            if (_myState == 1)
            {
                if (_blackScore == 0)
                {
                    return int.MinValue;
                }
                if (_whiteScore == 0)
                {
                    return int.MaxValue;
                }
                if (_whiteScore + _blackScore > 60 && _whiteScore < _blackScore)
                {
                    return int.MaxValue;
                }
                return _blackScore - _whiteScore;
            }
            else
            {
                if (_whiteScore == 0)
                {
                    return int.MinValue;
                }
                if (_blackScore == 0)
                {
                    return int.MaxValue;
                }
                if (_whiteScore + _blackScore > 60 && _whiteScore > _blackScore)
                {
                    return int.MaxValue;
                }
                return _whiteScore - _blackScore;
            }

        }

        /// <summary>
        /// Maximise my mobility and minimise enemy mobility
        /// </summary>
        /// <returns>a number calculate by my possible move minus the enemy possible move</returns>
        public int CalculateMobility()
        {
            int ownMobility = MyNumberPossibleMove-1;
            int enemyMobility = GetPossibleMove().Count();
            //Console.WriteLine("MY " + ownMobility + ", \t NOT MY " + enemyMobility);

            return ownMobility - enemyMobility;
        }

        /// <summary>
        /// Check if a position is in the border
        /// </summary>
        /// <param name="x">the x axis</param>
        /// <param name="y">the y axis</param>
        /// <returns>true if is in the board</returns>
        public bool IsBorder(int x, int y)
        {
            return (x == 0) || (y == 0) || (x == BOARDSIZE_X - 1) || (y == BOARDSIZE_Y - 1);
        }

        /// <summary>
        /// Check if a position is inside the board
        /// </summary>
        /// <param name="x">the x axis</param>
        /// <param name="y">the y axis</param>
        /// <returns>true if is in the board</returns>
        public bool IsInside(int x, int y)
        {
            return (x >= 0) && (y >= 0) && (x < BOARDSIZE_X) && (y < BOARDSIZE_Y);
        }

        /// <summary>
        /// Compute the tree to know what color is play
        /// </summary>
        /// <returns>My Color</returns>
        public bool MyColor()
        {
            Node node = this;
            while (node.Parent != null)
            {
                node = node.Parent;
            }
            return node.WhiteTurn;
        }

        /// <summary>
        /// Version 2 of the methode StableDisk but not working
        /// Could take more pieces.
        /// </summary>
        /// <returns>true if a corner is take</returns>
        public double StablePieces()
        {
            double numberStable = 0;

            if (IsStablePossible())
            {
                for (int i = 0; i < valueCell.GetLength(0); i++)
                {
                    for (int j = 0; j < valueCell.GetLength(1); j++)
                    {
                        if (IsStable(j, i))
                        {
                            if (CurrentBoard[j, i] == _myState)
                            {
                                numberStable += valueCell[i, j];
                            }
                            else if (CurrentBoard[j, i] == _enemiState)
                            {
                                numberStable -= valueCell[i, j];
                            }
                        }
                    }
                }
            }

            return numberStable;
        }

        /// <summary>
        /// For a piece can be stable one corner must be taken
        /// </summary>
        /// <returns>true if a corner is take</returns>
        public bool IsStablePossible()
        {
            var positionCorners = new (int, int)[] {  (0,0) ,
                                                    (0,6) ,
                                                    (8,0) ,
                                                    (8,6) };

            for (int i = 0; i < positionCorners.Length; i++)
            {
                if (CurrentBoard[positionCorners[i].Item1, positionCorners[i].Item2] != -1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Calcul if a piece is Stable. Cannot be flip anymore
        /// </summary>
        /// <param name="x">the x axis</param>
        /// <param name="y">the y axis</param>
        /// <returns>true if the piece in (x,y) are no more flipable</returns>
        public bool IsStable(int x, int y)
        {
            var directions = new (int, int)[] {  (0,1) ,
                                                    (0,-1) ,
                                                    (1,0) ,
                                                    (-1,0) ,
                                                    (1,1) ,
                                                    (-1,-1),
                                                    (-1,1) ,
                                                    (1,-1)};

            // int for the number of opposant pieces
            // bool to know if the direction is full (no more piece can be place)
            Tuple<int, bool>[] resultDirection = new Tuple<int, bool>[directions.Length];

            // Calcul the value
            for (int dir = 0; dir < directions.Length; dir++)
            {
                resultDirection[dir] = DirectionStable(directions[dir].Item1, directions[dir].Item2, x, y);
            }

            // No more place
            if (IsNoMorePlace(resultDirection))
            {
                return true;
            }

            // Other condition
            for (int i = 1; i < resultDirection.Length - 1; i++)
            {
                if (!IsOkey(resultDirection[i - 1].Item1, resultDirection[i].Item1, resultDirection[i + 1].Item1))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// No more place in each direction
        /// </summary>
        /// <param name="a">An array of Tuple</param>
        /// <returns>true if each direction has no more empty space</returns>
        public bool IsNoMorePlace(Tuple<int, bool>[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (!a[i].Item2)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check if 3 direction in a row have none opponent pieces
        /// </summary>
        /// <param name="number1">The first number</param>
        /// <param name="number2">The second number</param>
        /// <param name="number3">The third number</param>
        /// <returns>true if 3 direction in a row have no opponent pieces in</returns>
        public bool IsOkey(int number1, int number2, int number3)
        {
            return (number1 + number2 + number3 == 0) ? true : false;
        }

        /// <summary>
        /// Calcul if a direction has one opponent pieces and go to the end
        /// </summary>
        /// <param name="x">the x axis</param>
        /// <param name="y">the y axis</param>
        /// <param name="horizontal">direction of the axis x</param>
        /// <param name="vertical">direction of the axis y</param>
        /// <returns>A tuple of the number of opponent piece and if it hit the border</returns>
        public Tuple<int, bool> DirectionStable(int horizontal, int vertical, int x, int y)
        {
            x += horizontal;
            y += vertical;

            int counter = 0;

            while (IsInside(x, y))
            {
                if (CurrentBoard[x, y] == _enemiState)
                {
                    counter += 1;
                }
                else if (CurrentBoard[x, y] == -1)
                {
                    return new Tuple<int, bool>(counter, false);
                }
                x += horizontal;
                y += vertical;
            }

            return new Tuple<int, bool>(counter, true);
        }

        private void computeScore()
        {
            _whiteScore = 0;
            _blackScore = 0;
            foreach (var v in CurrentBoard)
            {
                if (v == (int)TileState.WHITE)
                    _whiteScore++;
                else if (v == (int)TileState.BLACK)
                    _blackScore++;
            }
            GameFinish = ((_whiteScore == 0) || (_blackScore == 0) ||
                        (_whiteScore + _blackScore == 63));
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
                        && (CurrentBoard[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        int counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0))
                                   && (CurrentBoard[c, l] == (int)opponent)) // pour éviter les trous
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (CurrentBoard[c, l] == (int)ownColor)
                            {
                                playable = true;
                                CurrentBoard[column, line] = (int)ownColor;
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
                    CurrentBoard[c, l] = (int)ownColor;
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
                    if (IsPlayable(i, j, WhiteTurn))
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
            if (CurrentBoard[column, line] != (int)TileState.EMPTY)
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
                        && (CurrentBoard[c, l] == (int)opponent))
                    // Verify if there is a friendly tile to "pinch" and return ennemy tiles in this direction
                    {
                        int counter = 0;
                        while (((c + dCol) < BOARDSIZE_X) && (c + dCol >= 0) &&
                                  ((l + dLine) < BOARDSIZE_Y) && ((l + dLine >= 0)))
                        {
                            c += dCol;
                            l += dLine;
                            counter++;
                            if (CurrentBoard[c, l] == (int)ownColor)
                            {
                                playable = true;
                                break;
                            }
                            else if (CurrentBoard[c, l] == (int)opponent)
                                continue;
                            else if (CurrentBoard[c, l] == (int)TileState.EMPTY)
                                break;  //empty slot ends the search
                        }
                    }
                }
            }
            return playable;
        }

        public void displayBoard(int[,] board = null)
        {
            Console.WriteLine("Whiteturn : " + this.WhiteTurn);
            if (board == null)
            {
                board = CurrentBoard;
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
                Node rootNode = new Node(theBoard.Clone() as int[,], whiteTurn, possibleMoves.Count());
                Tuple<double, Node>  result = alphabeta(rootNode, 5, 1, double.PositiveInfinity);
                if (result.Item2 == null)
                {
                    Console.WriteLine("passe le tour");
                    return new Tuple<int, int>(-1, -1);
                }
                return result.Item2.PossibleMove;
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
            List<Tuple<int, int>> possibleMoves = root.GetPossibleMove();
            foreach (var op in possibleMoves)
            {
                Node newNode = root.apply(op, possibleMoves.Count());
                Tuple<double, Node> valDummy = alphabeta(newNode, depth - 1, -minOrMax, optVal);
                if(valDummy.Item1 * minOrMax >= optVal * minOrMax)
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
