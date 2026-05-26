using System;
using System.Collections.Generic;
using System.Windows.Media;
using MyAIPet.Core.Mvvm;
using MyAIPet.Services;

namespace MyAIPet.ViewModels
{
    public class GobangViewModel : ViewModelBase
    {
        private readonly ChessBoard _board;
        private readonly ChessAI _ai;
        private int _currentPlayer;
        private bool _isGameOver;
        private string _statusText;
        private List<(int x, int y)> _winningLine;
        private (int x, int y) _lastMove;

        public int CurrentPlayer
        {
            get => _currentPlayer;
            set => SetProperty(ref _currentPlayer, value);
        }

        public bool IsGameOver
        {
            get => _isGameOver;
            set => SetProperty(ref _isGameOver, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public List<(int x, int y)> WinningLine
        {
            get => _winningLine;
            set => SetProperty(ref _winningLine, value);
        }

        public (int x, int y) LastMove
        {
            get => _lastMove;
            set => SetProperty(ref _lastMove, value);
        }

        public int[,] Board => _board.Board;
        public event Action? BoardUpdated;
        public event Action? GameEnded;

        public GobangViewModel()
        {
            _board = new ChessBoard();
            _ai = new ChessAI();
            _currentPlayer = ChessBoard.BLACK;
            _isGameOver = false;
            _statusText = "你的回合 (黑棋)";
            _winningLine = new List<(int x, int y)>();
            _lastMove = (-1, -1);
        }

        public bool PlaceStone(int x, int y)
        {
            if (_isGameOver || !_board.IsEmpty(x, y))
                return false;

            _board.PlaceStone(x, y, _currentPlayer);
            _lastMove = (x, y);
            BoardUpdated?.Invoke();

            var (isWin, line) = _board.CheckWin(x, y, _currentPlayer);
            if (isWin)
            {
                _isGameOver = true;
                _winningLine = line;
                StatusText = _currentPlayer == ChessBoard.BLACK ? "你赢了!" : "AI 获胜!";
                GameEnded?.Invoke();
                return true;
            }

            SwitchToAI();
            return true;
        }

        private void SwitchToAI()
        {
            _currentPlayer = ChessBoard.WHITE;
            StatusText = "AI 思考中...";

            var (aiX, aiY) = _ai.GetBestMove(_board, ChessBoard.WHITE);
            if (aiX >= 0 && aiY >= 0)
            {
                _board.PlaceStone(aiX, aiY, ChessBoard.WHITE);
                _lastMove = (aiX, aiY);
                BoardUpdated?.Invoke();

                var (isWin, line) = _board.CheckWin(aiX, aiY, ChessBoard.WHITE);
                if (isWin)
                {
                    _isGameOver = true;
                    _winningLine = line;
                    StatusText = "AI 获胜!";
                    GameEnded?.Invoke();
                    return;
                }
            }

            _currentPlayer = ChessBoard.BLACK;
            StatusText = "你的回合 (黑棋)";
        }

        public void Restart()
        {
            _board.Clear();
            _currentPlayer = ChessBoard.BLACK;
            _isGameOver = false;
            _statusText = "你的回合 (黑棋)";
            _winningLine = new List<(int x, int y)>();
            _lastMove = (-1, -1);
            BoardUpdated?.Invoke();
        }
    }
}