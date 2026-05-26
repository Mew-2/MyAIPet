using System;

namespace MyAIPet.Services
{
    public enum ChessSituation
    {
        WIN5,
        ALIVE4,
        DIE4,
        ALIVE3,
        DIE3,
        TIAO3,
        ALIVE2,
        DIE2,
        LOWALIVE2,
        LOWDIE4,
        NOTHREAT
    }

    public enum SearchLevel
    {
        LevelOne,
        Leveltwo,
        Levelthree,
        Levelfour,
        Levelfive,
        Levelsix,
        Levelseven,
        LevelEight,
        LevelNight,
        LevelTen,
        LevelEleven,
        LevelTwelve,
        LevelThirteen,
        LevelFourteen
    }

    public class ChessAI
    {
        private const int NOTHINGFLAG = 0;
        private const int MY_COLOR = 1;
        private const int HIS_COLOR = 2;

        private int[,] _chess;
        private int _myColor;
        private int _hisColor;

        private readonly int[][] _directions = new int[][]
        {
            new int[] { 1, 0 },
            new int[] { 0, 1 },
            new int[] { 1, 1 },
            new int[] { 1, -1 }
        };

        public (int x, int y) GetBestMove(ChessBoard board, int aiColor)
        {
            _chess = board.Board;
            _myColor = aiColor;
            _hisColor = aiColor == ChessBoard.BLACK ? ChessBoard.WHITE : ChessBoard.BLACK;

            var emptyPositions = board.GetAllEmptyPositions();

            foreach (var (x, y) in emptyPositions)
            {
                _chess[x, y] = _myColor;
                bool myWin5 = CheckWinAt(x, y, _myColor);
                _chess[x, y] = ChessBoard.EMPTY;
                if (myWin5)
                {
                    return (x, y);
                }
            }

            foreach (var (x, y) in emptyPositions)
            {
                _chess[x, y] = _hisColor;
                bool opponentWin5 = CheckWinAt(x, y, _hisColor);
                _chess[x, y] = ChessBoard.EMPTY;
                if (opponentWin5)
                {
                    return (x, y);
                }
            }

            foreach (var (x, y) in emptyPositions)
            {
                _chess[x, y] = _hisColor;
                bool opponentAlive4 = CheckAlive4At(x, y, _hisColor);
                _chess[x, y] = ChessBoard.EMPTY;
                if (opponentAlive4)
                {
                    return (x, y);
                }
            }

            int bestX = -1, bestY = -1;
            int bestScore = int.MinValue;

            foreach (var (x, y) in emptyPositions)
            {
                int score = EvaluatePosition(x, y);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestX = x;
                    bestY = y;
                }
            }

            return (bestX, bestY);
        }

        private bool CheckWinAt(int x, int y, int color)
        {
            foreach (var dir in _directions)
            {
                if (CountInDirection(x, y, color, dir[0], dir[1]) >= 5)
                    return true;
            }
            return false;
        }

        private bool CheckAlive4At(int x, int y, int color)
        {
            foreach (var dir in _directions)
            {
                var situation = EvaluateDirection(x, y, color, color == _myColor ? _hisColor : _myColor, dir[0], dir[1]);
                if (situation == ChessSituation.ALIVE4)
                    return true;
            }
            return false;
        }

        private int CountInDirection(int x, int y, int color, int dx, int dy)
        {
            int count = 1;
            int lx = x - dx, ly = y - dy;
            while (IsInBoard(lx, ly) && _chess[lx, ly] == color)
            {
                count++;
                lx -= dx;
                ly -= dy;
            }
            int rx = x + dx, ry = y + dy;
            while (IsInBoard(rx, ry) && _chess[rx, ry] == color)
            {
                count++;
                rx += dx;
                ry += dy;
            }
            return count;
        }

        private int EvaluatePosition(int x, int y)
        {
            int myAttackScore = 0;
            int hisAttackScore = 0;

            foreach (var dir in _directions)
            {
                var mySituation = EvaluateDirection(x, y, _myColor, _hisColor, dir[0], dir[1]);
                var hisSituation = EvaluateDirection(x, y, _hisColor, _myColor, dir[0], dir[1]);

                myAttackScore += GetScore(mySituation);
                hisAttackScore += (int)(GetScore(hisSituation) * 1.2f);
            }

            return Math.Max(myAttackScore, hisAttackScore);
        }

        private ChessSituation EvaluateDirection(int x, int y, int mycolor, int hiscolor, int dx, int dy)
        {
            int count = 1;
            int lx = x - dx, ly = y - dy;
            while (IsInBoard(lx, ly) && _chess[lx, ly] == mycolor)
            {
                count++;
                lx -= dx;
                ly -= dy;
            }
            int colorleft = IsInBoard(lx, ly) ? _chess[lx, ly] : -1;

            int rx = x + dx, ry = y + dy;
            while (IsInBoard(rx, ry) && _chess[rx, ry] == mycolor)
            {
                count++;
                rx += dx;
                ry += dy;
            }
            int colorright = IsInBoard(rx, ry) ? _chess[rx, ry] : -1;

            return EvaluatePattern(count, colorleft, colorright, mycolor, hiscolor, x - dx, y - dy, x + dx, y + dy, dx, dy);
        }

        private bool IsInBoard(int x, int y)
        {
            return x >= 0 && x < ChessBoard.BOARD_SIZE && y >= 0 && y < ChessBoard.BOARD_SIZE;
        }

        private ChessSituation EvaluatePattern(int count, int colorleft, int colorright, int mycolor, int hiscolor, int leftX, int leftY, int rightX, int rightY, int dx, int dy)
        {
            if (count >= 5)
                return ChessSituation.WIN5;

            if (count == 4)
            {
                if (colorleft == NOTHINGFLAG && colorright == NOTHINGFLAG)
                    return ChessSituation.ALIVE4;
                else if (colorleft == hiscolor && colorright == hiscolor)
                    return ChessSituation.NOTHREAT;
                else if (colorleft == NOTHINGFLAG || colorright == NOTHINGFLAG)
                    return ChessSituation.DIE4;
            }

            if (count == 3)
            {
                int colorleft1 = GetChess(leftX - dx, leftY - dy);
                int colorright1 = GetChess(rightX + dx, rightY + dy);

                if (colorleft == NOTHINGFLAG && colorright == NOTHINGFLAG)
                {
                    if (colorleft1 == hiscolor && colorright1 == hiscolor)
                        return ChessSituation.DIE3;
                    else if (colorleft1 == mycolor || colorright1 == mycolor)
                        return ChessSituation.LOWDIE4;
                    else if (colorleft1 == NOTHINGFLAG || colorright1 == NOTHINGFLAG)
                        return ChessSituation.ALIVE3;
                }
                else if (colorleft == hiscolor && colorright == hiscolor)
                {
                    return ChessSituation.NOTHREAT;
                }
                else if (colorleft == NOTHINGFLAG || colorright == NOTHINGFLAG)
                {
                    if (colorleft == hiscolor)
                    {
                        if (colorright1 == hiscolor)
                            return ChessSituation.NOTHREAT;
                        if (colorright1 == NOTHINGFLAG)
                            return ChessSituation.DIE3;
                        if (colorright1 == mycolor)
                            return ChessSituation.LOWDIE4;
                    }
                    if (colorright == hiscolor)
                    {
                        if (colorleft1 == hiscolor)
                            return ChessSituation.NOTHREAT;
                        if (colorleft1 == NOTHINGFLAG)
                            return ChessSituation.DIE3;
                        if (colorleft1 == mycolor)
                            return ChessSituation.LOWDIE4;
                    }
                }
            }

            if (count == 2)
            {
                int colorleft1 = GetChess(leftX - dx, leftY - dy);
                int colorright1 = GetChess(rightX + dx, rightY + dy);
                int colorleft2 = GetChess(leftX - dx * 2, leftY - dy * 2);
                int colorright2 = GetChess(rightX + dx * 2, rightY + dy * 2);

                if (colorleft == NOTHINGFLAG && colorright == NOTHINGFLAG)
                {
                    if ((colorright1 == NOTHINGFLAG && colorright2 == mycolor) ||
                        (colorleft1 == NOTHINGFLAG && colorleft2 == mycolor))
                        return ChessSituation.DIE3;
                    else if (colorleft1 == NOTHINGFLAG && colorright1 == NOTHINGFLAG)
                        return ChessSituation.ALIVE2;

                    if ((colorright1 == mycolor && colorright2 == hiscolor) ||
                        (colorleft1 == mycolor && colorleft2 == hiscolor))
                        return ChessSituation.DIE3;

                    if ((colorright1 == mycolor && colorright2 == mycolor) ||
                        (colorleft1 == mycolor && colorleft2 == mycolor))
                        return ChessSituation.LOWDIE4;

                    if ((colorright1 == mycolor && colorright2 == NOTHINGFLAG) ||
                        (colorleft1 == mycolor && colorleft2 == NOTHINGFLAG))
                        return ChessSituation.TIAO3;
                }
                else if (colorleft == hiscolor && colorright == hiscolor)
                {
                    return ChessSituation.NOTHREAT;
                }
                else if (colorleft == NOTHINGFLAG || colorright == NOTHINGFLAG)
                {
                    if (colorleft == hiscolor)
                    {
                        if (colorright1 == hiscolor || colorright2 == hiscolor)
                            return ChessSituation.NOTHREAT;
                        else if (colorright1 == NOTHINGFLAG && colorright2 == NOTHINGFLAG)
                            return ChessSituation.DIE2;
                        else if (colorright1 == mycolor && colorright2 == mycolor)
                            return ChessSituation.LOWDIE4;
                        else if (colorright1 == mycolor || colorright2 == mycolor)
                            return ChessSituation.DIE3;
                    }
                    if (colorright == hiscolor)
                    {
                        if (colorleft1 == hiscolor || colorleft2 == hiscolor)
                            return ChessSituation.NOTHREAT;
                        else if (colorleft1 == NOTHINGFLAG && colorleft2 == NOTHINGFLAG)
                            return ChessSituation.DIE2;
                        else if (colorleft1 == mycolor && colorleft2 == mycolor)
                            return ChessSituation.LOWDIE4;
                        else if (colorleft1 == mycolor || colorleft2 == mycolor)
                            return ChessSituation.DIE3;
                    }
                }
            }

            if (count == 1)
            {
                int colorleft1 = GetChess(leftX - dx, leftY - dy);
                int colorright1 = GetChess(rightX + dx, rightY + dy);
                int colorleft2 = GetChess(leftX - dx * 2, leftY - dy * 2);
                int colorright2 = GetChess(rightX + dx * 2, rightY + dy * 2);
                int colorleft3 = GetChess(leftX - dx * 3, leftY - dy * 3);
                int colorright3 = GetChess(rightX + dx * 3, rightY + dy * 3);

                if (colorleft == NOTHINGFLAG && colorleft1 == mycolor &&
                    colorleft2 == mycolor && colorleft3 == mycolor)
                    return ChessSituation.LOWDIE4;
                if (colorright == NOTHINGFLAG && colorright1 == mycolor &&
                    colorright2 == mycolor && colorright3 == mycolor)
                    return ChessSituation.LOWDIE4;

                if (colorleft == NOTHINGFLAG && colorleft1 == mycolor &&
                    colorleft2 == mycolor && colorleft3 == NOTHINGFLAG && colorright == NOTHINGFLAG)
                    return ChessSituation.TIAO3;
                if (colorright == NOTHINGFLAG && colorright1 == mycolor &&
                    colorright2 == mycolor && colorright3 == NOTHINGFLAG && colorleft == NOTHINGFLAG)
                    return ChessSituation.TIAO3;

                if (colorleft == NOTHINGFLAG && colorleft1 == mycolor &&
                    colorleft2 == mycolor && colorleft3 == hiscolor && colorright == NOTHINGFLAG)
                    return ChessSituation.DIE3;
                if (colorright == NOTHINGFLAG && colorright1 == mycolor &&
                    colorright2 == mycolor && colorright3 == hiscolor && colorleft == NOTHINGFLAG)
                    return ChessSituation.DIE3;

                if (colorleft == NOTHINGFLAG && colorleft1 == NOTHINGFLAG &&
                    colorleft2 == mycolor && colorleft3 == mycolor)
                    return ChessSituation.DIE3;
                if (colorright == NOTHINGFLAG && colorright1 == NOTHINGFLAG &&
                    colorright2 == mycolor && colorright3 == mycolor)
                    return ChessSituation.DIE3;

                if (colorleft == NOTHINGFLAG && colorleft1 == mycolor &&
                    colorleft2 == NOTHINGFLAG && colorleft3 == mycolor)
                    return ChessSituation.DIE3;
                if (colorright == NOTHINGFLAG && colorright1 == mycolor &&
                    colorright2 == NOTHINGFLAG && colorright3 == mycolor)
                    return ChessSituation.DIE3;

                if (colorleft == NOTHINGFLAG && colorleft1 == mycolor &&
                    colorleft2 == NOTHINGFLAG && colorleft3 == NOTHINGFLAG && colorright == NOTHINGFLAG)
                    return ChessSituation.LOWALIVE2;
                if (colorright == NOTHINGFLAG && colorright1 == mycolor &&
                    colorright2 == NOTHINGFLAG && colorright3 == NOTHINGFLAG && colorleft == NOTHINGFLAG)
                    return ChessSituation.LOWALIVE2;

                if (colorleft == NOTHINGFLAG && colorleft1 == NOTHINGFLAG &&
                    colorleft2 == mycolor && colorleft3 == NOTHINGFLAG && colorright == NOTHINGFLAG)
                    return ChessSituation.LOWALIVE2;
                if (colorright == NOTHINGFLAG && colorright1 == NOTHINGFLAG &&
                    colorright2 == mycolor && colorright3 == NOTHINGFLAG && colorleft == NOTHINGFLAG)
                    return ChessSituation.LOWALIVE2;
            }

            return ChessSituation.NOTHREAT;
        }

        private int GetChess(int x, int y)
        {
            if (!IsInBoard(x, y))
                return -1;
            return _chess[x, y];
        }

        private int GetScore(ChessSituation situation)
        {
            return situation switch
            {
                ChessSituation.WIN5 => 100000,
                ChessSituation.ALIVE4 => 10000,
                ChessSituation.DIE4 => 1000,
                ChessSituation.ALIVE3 => 1000,
                ChessSituation.DIE3 => 100,
                ChessSituation.TIAO3 => 100,
                ChessSituation.ALIVE2 => 100,
                ChessSituation.DIE2 => 10,
                ChessSituation.LOWALIVE2 => 10,
                ChessSituation.LOWDIE4 => 500,
                _ => 0
            };
        }

        public SearchLevel GetLevel(ChessBoard board, int color)
        {
            _chess = board.Board;
            _myColor = color;
            _hisColor = color == ChessBoard.BLACK ? ChessBoard.WHITE : ChessBoard.BLACK;

            int win5 = 0, alive4 = 0, die4 = 0, alive3 = 0, die3 = 0, tiao3 = 0, alive2 = 0, die2 = 0, lowalive2 = 0, lowdie4 = 0;

            for (int i = 0; i < ChessBoard.BOARD_SIZE; i++)
            {
                for (int j = 0; j < ChessBoard.BOARD_SIZE; j++)
                {
                    if (_chess[i, j] != ChessBoard.EMPTY)
                        continue;

                    foreach (var dir in _directions)
                    {
                        var situation = EvaluateDirection(i, j, _myColor, _hisColor, dir[0], dir[1]);
                        switch (situation)
                        {
                            case ChessSituation.WIN5: win5++; break;
                            case ChessSituation.ALIVE4: alive4++; break;
                            case ChessSituation.DIE4: die4++; break;
                            case ChessSituation.ALIVE3: alive3++; break;
                            case ChessSituation.DIE3: die3++; break;
                            case ChessSituation.TIAO3: tiao3++; break;
                            case ChessSituation.ALIVE2: alive2++; break;
                            case ChessSituation.DIE2: die2++; break;
                            case ChessSituation.LOWALIVE2: lowalive2++; break;
                            case ChessSituation.LOWDIE4: lowdie4++; break;
                        }
                    }
                }
            }

            if (win5 >= 1) return SearchLevel.LevelOne;
            if (alive4 >= 1 || die4 >= 2 || (die4 >= 1 && alive3 >= 1)) return SearchLevel.Leveltwo;
            if (alive3 >= 2) return SearchLevel.Levelthree;
            if (die3 >= 1 && alive3 >= 1) return SearchLevel.Levelfour;
            if (die4 >= 1) return SearchLevel.Levelfive;
            if (lowdie4 >= 1) return SearchLevel.Levelsix;
            if (alive3 >= 1) return SearchLevel.Levelseven;
            if (tiao3 >= 1) return SearchLevel.LevelEight;
            if (alive2 >= 2) return SearchLevel.LevelNight;
            if (alive2 >= 1) return SearchLevel.LevelTen;
            if (lowalive2 >= 1) return SearchLevel.LevelEleven;
            if (die3 >= 1) return SearchLevel.LevelTwelve;
            if (die2 >= 1) return SearchLevel.LevelThirteen;
            return SearchLevel.LevelFourteen;
        }
    }
}