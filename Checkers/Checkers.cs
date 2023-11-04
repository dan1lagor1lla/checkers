namespace CheckersLibrary
{
    public class Checkers : object
    {
        public Board Board { get; }
        public CheckersSetting Settings { get; }
        public Team WhoseMove { get; private set; }

        public Checkers() : this(CheckersSetting.International) { }
        public Checkers(CheckersSetting settings)
        {
            Settings = settings;
            Board = new Board(this);
            WhoseMove = Settings.FirstMove;
        }

        internal void EndMove()
        {
            WhoseMove = WhoseMove == Team.White ? Team.Black : Team.White;
            Board.UpdateSquaresAvailableMoves();
        }
    }

    public class Board : object
    {
        private readonly Square[] squares;

        public Checkers SourceGame { get; }
        internal int MaxMovePower { get; set; } = 0;
        public int Size => SourceGame.Settings.BoardSize;

        public Square[] AllSquares => squares.ToArray();
        public List<Square> AllSquaresList => squares.ToList();

        internal Board(Checkers sourceGame)
        {
            SourceGame = sourceGame;
            squares = new Square[Size * Size / 2];
            for (int row = 1, column = 2, number = 0; row <= Size; number++, row += column >= Size - 1 ? 1 : 0, column = column >= Size - 1 ? (row % 2 == 0 ? 1 : 2) : column + 2)
                squares[number] = new Square(this, number + 1, new Position(row, column));
            UpdateSquaresAvailableMoves();
        }

        public Square? TryGetSquare(Position position) => TryGetSquare(position.Row, position.Column);
        public Square? TryGetSquare(int row, int column) // to do: getSquare()
        { 
            if (!SquareExist(row, column))
                return null;
            //return squares.First((square) => square.Position.Row == row && square.Position.Column == column);
            foreach (Square square in squares)
                if (square.Position.Row == row && square.Position.Column == column)
                    return square;
            return null;
        }
        public Square? TryGetSquare(int number) // to implement
        {
            if (!SquareExist(number)) 
                return null;
            //return squares.First((square) => square.Number == number);
            Square square = null!;
            foreach (Square item in squares)
                if (item.Number == number)
                    square = item;
            return square;
        }

        internal void UpdateSquaresAvailableMoves()
        {
            MaxMovePower = 0;
            foreach (Square square in squares)
                if (square.Content?.Team == SourceGame.WhoseMove)
                    square.Content.UpdateAvailableMoves();
        }

        public bool SquareExist(int row, int column) => !(row < 1 || column < 1 || row > Size || column > Size);
        public bool SquareExist(Position position) => SquareExist(position.Row, position.Column);
        public bool SquareExist(int number) => number > 0 && number <= Size * Size / 2;
    }

    public class Square : object
    {
        public int Number { get; }
        public Position Position { get; }
        public Board SourceBoard { get; }
        public Checker? Content { get; internal set; }

        internal Square(Board sourceBoard, int number, Position position)
        {
            SourceBoard = sourceBoard;
            Position = position;
            Number = number;
            if (Number <= SourceBoard.SourceGame.Settings.PiecesPerSideInitial)
                Content = new Checker(this, Team.White);
            else if (Number > SourceBoard.Size * SourceBoard.Size / 2 - SourceBoard.SourceGame.Settings.PiecesPerSideInitial)
                Content = new Checker(this, Team.Black);
        }
    }

    public class Checker : object
    {
        private List<CheckerMove> availableMoves;

        public Team Team { get; }
        public bool IsKing { get; private set; }
        public Square? Location { get; private set; }
        public Board? SourceBoard => Location?.SourceBoard;
        public Checkers? SourceGame => SourceBoard?.SourceGame;

        public List<CheckerMove>? AvailableMoves
        {
            get
            {
                if (SourceGame?.WhoseMove != Team)
                    return null;
                if (SourceGame!.Settings.MustCaptureMaximum)
                    return new List<CheckerMove>(availableMoves.Where((move) => move.MovePower == SourceBoard!.MaxMovePower));
                if (SourceBoard!.MaxMovePower > 0)
                   return new List<CheckerMove>(availableMoves.Where((move) => move.MovePower > 0));
                return availableMoves.ToList();
            }
        }

        internal Checker(Square location, Team team)
        {
            Team = team;
            Location = location;
            IsKing = false;
            availableMoves = new List<CheckerMove>();
        }

        private void RemoveFromBoard()
        {
            if (Location == null)
                return;
            Location.Content = null;
            Location = null;
            availableMoves.Clear();
        }

        public bool TryMoveTo(Square square)
        {
            if (Location == null)
                return false;
            CheckerMove? move = AvailableMoves?.FirstOrDefault((move) => move?.Destination == square, null);
            if (move is null)
                return false;
            Location.Content = null;
            Location = square;
            Location.Content = this;
            if (move.SquaresToCapture is not null) 
                foreach (Square capturedSquares in move.SquaresToCapture)
                    capturedSquares.Content!.RemoveFromBoard();
            if ((Team == Team.Black && move.Destination.Position.Row == 1) || (Team == Team.White && move.Destination.Position.Row == SourceBoard!.Size))
                IsKing = true;
            SourceGame!.EndMove();
            return true;
        }

        internal void UpdateAvailableMoves()
        {
            availableMoves.Clear();
            availableMoves.AddRange(GetCaptureMoves(Location, new List<Square>()));

            if (availableMoves.Count != 0)
            {
                int maxMovePower = availableMoves.Max((move) => move.MovePower);
                if (maxMovePower > SourceBoard!.MaxMovePower)
                    SourceBoard.MaxMovePower = maxMovePower;
                return;
            }

            if (IsKing)
            {
                for (int rowChange = -1, columnChange = -1; rowChange <= 1; columnChange += 2)
                {
                    if (columnChange == 3)
                    {
                        rowChange += 2;
                        columnChange = -3;
                        continue;
                    }
                    Square? availableMove = Location.SourceBoard.TryGetSquare(Location.Position.Row + rowChange, Location.Position.Column + columnChange);
                    for (int count = 2; availableMove != null && availableMove.Content == null; count++)
                    {
                        availableMoves.Add(new CheckerMove(availableMove));
                        availableMove = Location.SourceBoard.TryGetSquare(Location.Position.Row + rowChange * count, Location.Position.Column + columnChange * count);
                    }
                }
            }
            else
            {
                for (int columnChange = -1; columnChange <= 1; columnChange += 2)
                {
                    Square? availableMove = Location.SourceBoard.TryGetSquare(Location.Position.Row + (Team == Team.White ? 1 : -1), Location.Position.Column + columnChange);
                    if (availableMove != null && availableMove.Content == null)
                        availableMoves.Add(new CheckerMove(availableMove));
                }
            }

            List<CheckerMove> GetCaptureMoves(Square startSquare, List<Square> capturedSquares)
            {
                List<CheckerMove> moves = new List<CheckerMove>();
                for (int rowChange = -1, columnChange = -1; rowChange <= 1; columnChange += 2)
                {
                    if (columnChange == 3)
                    {
                        rowChange += 2;
                        columnChange = -3;
                        continue;
                    }
                    Square? captureSquare = startSquare.SourceBoard.TryGetSquare(startSquare.Position.Row + rowChange, startSquare.Position.Column + columnChange);
                    Square? destinationSquare = startSquare.SourceBoard.TryGetSquare(startSquare.Position.Row + rowChange * 2, startSquare.Position.Column + columnChange * 2);

                    if (IsKing)
                    {
                        int count = 0, row = 0, column = 0;
                        while (true)
                        {
                            count++;
                            row = startSquare.Position.Row + rowChange * count;
                            column = startSquare.Position.Column + columnChange * count;
                            captureSquare = SourceBoard.TryGetSquare(row, column);
                            if (captureSquare == null || capturedSquares.Contains(captureSquare))
                                break;
                            if (captureSquare.Content == null)
                                continue;
                            if (captureSquare.Content.Team == Team)
                                break;
                            for (int count2 = 1; ; count2++)
                            {
                                destinationSquare = SourceBoard.TryGetSquare(row + rowChange * count2, column + columnChange * count2);
                                if (destinationSquare == null || destinationSquare.Content != null)
                                    break;
                                moves.Add(new CheckerMove(destinationSquare, new List<Square>(capturedSquares) { captureSquare }));
                                moves.AddRange(GetCaptureMoves(destinationSquare, new List<Square>(capturedSquares) { captureSquare }));
                            }
                            break;
                        }
                    }
                    else
                    {
                        captureSquare = startSquare.SourceBoard.TryGetSquare(startSquare.Position.Row + rowChange, startSquare.Position.Column + columnChange);
                        destinationSquare = startSquare.SourceBoard.TryGetSquare(startSquare.Position.Row + rowChange * 2, startSquare.Position.Column + columnChange * 2);

                        if (captureSquare == null || destinationSquare == null || captureSquare.Content == null || captureSquare.Content.Team == Team || (destinationSquare.Content != null && destinationSquare.Content != this) || capturedSquares.Contains(captureSquare!))
                            continue;

                        moves.Add(new CheckerMove(destinationSquare, new List<Square>(capturedSquares) { captureSquare }));
                        moves.AddRange(GetCaptureMoves(destinationSquare, new List<Square>(capturedSquares) { captureSquare }));
                    }
                }
                return moves;
            }
        }
    }

    public class CheckerMove
    {
        public Square Destination { get; }
        public List<Square>? SquaresToCapture { get; }
        public int MovePower => SquaresToCapture is null ? 0 : SquaresToCapture.Count;

        internal CheckerMove(Square destination, List<Square>? squaresToCapture = null)
        {
            Destination = destination;
            SquaresToCapture = squaresToCapture;
        }
    }

    public struct CheckersSetting
    {
        public int BoardSize { get; }
        public int PiecesPerSideInitial { get; }
        public bool MustCaptureMaximum { get; }
        public Team FirstMove { get; }
        

        public CheckersSetting() : this(International) { }

        public CheckersSetting(CheckersSetting settings)
        {
            BoardSize = settings.BoardSize;
            PiecesPerSideInitial = settings.PiecesPerSideInitial;
            FirstMove = settings.FirstMove;
        }

        private CheckersSetting(int boardSize, int piecesPerSide, bool mustCaptureMaximum, Team firstMove)
        {
            BoardSize = boardSize;
            PiecesPerSideInitial = piecesPerSide;
            FirstMove = firstMove;
            MustCaptureMaximum = mustCaptureMaximum;
        }

        public static CheckersSetting International => new CheckersSetting(10, 20, true, Team.White);
        public static CheckersSetting Russian => new CheckersSetting(8, 12, false, Team.White);
        public static CheckersSetting Canadian => new CheckersSetting(12, 30, true, Team.White);
    }

    public struct Position
    {
        public int Row { get; set; }
        public int Column { get; set; }

        public Position() : this(1, 1) { }

        public Position(int row, int column)
        {
            Row = row;
            Column = column;
        }
    }

    public enum Team : int
    {
        White = 0,
        Black = 1
    }
}