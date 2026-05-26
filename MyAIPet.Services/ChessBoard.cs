using System;
using System.Collections.Generic;

namespace MyAIPet.Services
{
    public class ChessBoard
    {
        public const int BOARD_SIZE = 15;
        public const int EMPTY = 0;
        public const int BLACK = 1;
        public const int WHITE = 2;

        private int[,] _board;

        public int[,] Board => _board;

        public ChessBoard()
        {
            _board = new int[BOARD_SIZE, BOARD_SIZE];
        }

        public ChessBoard(int[,] board)
        {
            _board = (int[,])board.Clone();
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < BOARD_SIZE && y >= 0 && y < BOARD_SIZE;
        }

        public bool IsEmpty(int x, int y)
        {
            return IsValidPosition(x, y) && _board[x, y] == EMPTY;
        }

        public bool PlaceStone(int x, int y, int color)
        {
            if (!IsEmpty(x, y))
                return false;
            _board[x, y] = color;
            return true;
        }

        public void RemoveStone(int x, int y)
        {
            if (IsValidPosition(x, y))
                _board[x, y] = EMPTY;
        }

        public int GetStone(int x, int y)
        {
            if (!IsValidPosition(x, y))
                return -1;
            return _board[x, y];
        }

        public void Clear()
        {
            Array.Clear(_board, 0, _board.Length);
        }

        public List<(int x, int y)> GetAllEmptyPositions()
        {
            var positions = new List<(int x, int y)>();
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    if (_board[i, j] == EMPTY)
                        positions.Add((i, j));
                }
            }
            return positions;
        }

        public bool HasStoneAt(int x, int y, int color)
        {
            return IsValidPosition(x, y) && _board[x, y] == color;
        }

        public (bool isWin, List<(int x, int y)> winningLine) CheckWin(int lastX, int lastY, int color)
        {
            if (lastX < 0 || lastY < 0)
                return (false, new List<(int x, int y)>());

            var directions = new (int dx, int dy)[]
            {
                (1, 0),
                (0, 1),
                (1, 1),
                (1, -1)
            };

            foreach (var (dx, dy) in directions)
            {
                var line = GetConnectLine(lastX, lastY, color, dx, dy);
                if (line.Count >= 5)
                    return (true, line);
            }

            return (false, new List<(int x, int y)>());
        }

        private List<(int x, int y)> GetConnectLine(int x, int y, int color, int dx, int dy)
        {
            var line = new List<(int x, int y)>();
            int count = 0;

            for (int i = -4; i <= 4; i++)
            {
                int nx = x + i * dx;
                int ny = y + i * dy;

                if (IsValidPosition(nx, ny) && _board[nx, ny] == color)
                {
                    count++;
                }
                else
                {
                    if (count >= 5)
                    {
                        for (int j = i - count; j < i; j++)
                        {
                            line.Add((x + j * dx, y + j * dy));
                        }
                    }
                    count = 0;
                }
            }

            if (count >= 5)
            {
                for (int i = 5 - count; i <= 0; i++)
                {
                    line.Add((x + i * dx, y + i * dy));
                }
                for (int i = 0; i < count; i++)
                {
                    line.Add((x + i * dx, y + i * dy));
                }
            }

            return line;
        }
    }
}