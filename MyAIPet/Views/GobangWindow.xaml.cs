using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using MyAIPet.Services;
using MyAIPet.ViewModels;

namespace MyAIPet.Views
{
    public partial class GobangWindow : Window
    {
        private readonly GobangViewModel _viewModel;
        private const int BOARD_SIZE = 15;
        private const double CELL_SIZE = 36;
        private const double PADDING = 18;
        private const double STONE_SIZE = 31;

        private Ellipse? _hoverIndicator;
        private int _hoverX = -1;
        private int _hoverY = -1;
        private Ellipse? _pulseIndicator;

        public GobangWindow()
        {
            InitializeComponent();
            _viewModel = new GobangViewModel();
            DataContext = _viewModel;
            
            _viewModel.BoardUpdated += OnBoardUpdated;
            _viewModel.GameEnded += OnGameEnded;
            
            DrawBoard();
            UpdateBoardDisplay();
        }

        private void DrawPulseIndicator()
        {
            if (_pulseIndicator != null)
                return;

            _pulseIndicator = new Ellipse
            {
                Width = STONE_SIZE * 1.4,
                Height = STONE_SIZE * 1.4,
                Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                StrokeThickness = 3,
                Fill = Brushes.Transparent,
                Opacity = 0.8,
                IsHitTestVisible = false
            };

            var scaleTransform = new ScaleTransform(1, 1);
            _pulseIndicator.RenderTransform = scaleTransform;
            _pulseIndicator.RenderTransformOrigin = new Point(0.5, 0.5);

            var scaleAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(750),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            var opacityAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 0.3,
                Duration = TimeSpan.FromMilliseconds(750),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            _pulseIndicator.BeginAnimation(OpacityProperty, opacityAnimation);

            BoardCanvas.Children.Add(_pulseIndicator);
        }

        private void DrawBoard()
        {
            var gridBrush = new SolidColorBrush(Color.FromRgb(0x7F, 0xB3, 0xD5));
            
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                var hLine = new Line
                {
                    X1 = PADDING,
                    Y1 = PADDING + i * CELL_SIZE,
                    X2 = PADDING + (BOARD_SIZE - 1) * CELL_SIZE,
                    Y2 = PADDING + i * CELL_SIZE,
                    Stroke = gridBrush,
                    StrokeThickness = 1,
                    SnapsToDevicePixels = true
                };
                BoardCanvas.Children.Add(hLine);

                var vLine = new Line
                {
                    X1 = PADDING + i * CELL_SIZE,
                    Y1 = PADDING,
                    X2 = PADDING + i * CELL_SIZE,
                    Y2 = PADDING + (BOARD_SIZE - 1) * CELL_SIZE,
                    Stroke = gridBrush,
                    StrokeThickness = 1,
                    SnapsToDevicePixels = true
                };
                BoardCanvas.Children.Add(vLine);
            }

            var starPoints = new (int x, int y)[] { (3, 3), (11, 3), (7, 7), (3, 11), (11, 11) };
            var starBrush = new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C));
            foreach (var (x, y) in starPoints)
            {
                var star = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Stroke = starBrush,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(star, PADDING + x * CELL_SIZE - 5);
                Canvas.SetTop(star, PADDING + y * CELL_SIZE - 5);
                BoardCanvas.Children.Add(star);
            }
        }

        private void BoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.IsGameOver)
                return;

            var pos = e.GetPosition(BoardCanvas);
            int x = (int)Math.Round((pos.X - PADDING) / CELL_SIZE);
            int y = (int)Math.Round((pos.Y - PADDING) / CELL_SIZE);

            if (x >= 0 && x < BOARD_SIZE && y >= 0 && y < BOARD_SIZE)
            {
                if (_viewModel.PlaceStone(x, y))
                {
                    HideHoverIndicator();
                    UpdateBoardDisplay();
                }
            }
        }

        private void BoardCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel.IsGameOver)
            {
                HideHoverIndicator();
                return;
            }

            var pos = e.GetPosition(BoardCanvas);
            int x = (int)Math.Round((pos.X - PADDING) / CELL_SIZE);
            int y = (int)Math.Round((pos.Y - PADDING) / CELL_SIZE);

            if (x >= 0 && x < BOARD_SIZE && y >= 0 && y < BOARD_SIZE)
            {
                if (x != _hoverX || y != _hoverY)
                {
                    _hoverX = x;
                    _hoverY = y;
                    
                    if (_viewModel.Board[x, y] == ChessBoard.EMPTY)
                    {
                        ShowHoverIndicator(x, y);
                    }
                    else
                    {
                        HideHoverIndicator();
                    }
                }
            }
            else
            {
                HideHoverIndicator();
            }
        }

        private void BoardCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            HideHoverIndicator();
        }

        private void ShowHoverIndicator(int x, int y)
        {
            if (_hoverIndicator == null)
            {
                _hoverIndicator = new Ellipse
                {
                    Width = STONE_SIZE,
                    Height = STONE_SIZE,
                    SnapsToDevicePixels = true
                };
                BoardCanvas.Children.Add(_hoverIndicator);
            }

            var isBlack = _viewModel.CurrentPlayer == ChessBoard.BLACK;
            _hoverIndicator.Fill = isBlack 
                ? new SolidColorBrush(Color.FromArgb(77, 0x2C, 0x3E, 0x50))
                : new SolidColorBrush(Color.FromArgb(128, 0xFF, 0xFA, 0xFA));

            Canvas.SetLeft(_hoverIndicator, PADDING + x * CELL_SIZE - STONE_SIZE / 2);
            Canvas.SetTop(_hoverIndicator, PADDING + y * CELL_SIZE - STONE_SIZE / 2);
            _hoverIndicator.Visibility = Visibility.Visible;
        }

        private void HideHoverIndicator()
        {
            if (_hoverIndicator != null)
            {
                _hoverIndicator.Visibility = Visibility.Collapsed;
            }
            _hoverX = -1;
            _hoverY = -1;
        }

        private void OnBoardUpdated()
        {
            Dispatcher.Invoke(UpdateBoardDisplay);
        }

        private void OnGameEnded()
        {
            Dispatcher.Invoke(() =>
            {
                HideHoverIndicator();
                if (_pulseIndicator != null)
                {
                    BoardCanvas.Children.Remove(_pulseIndicator);
                    _pulseIndicator = null;
                }
                UpdateBoardDisplay();
                DrawWinningLine();
                StatusText.Text = _viewModel.StatusText;
            });
        }

        private void UpdateBoardDisplay()
        {
            var toRemove = new System.Collections.Generic.List<System.Windows.UIElement>();
            
            foreach (var child in BoardCanvas.Children)
            {
                if (child is Line line)
                {
                    if (line.StrokeThickness == 1)
                        continue;
                    if (line.StrokeThickness == 6 || line.StrokeThickness == 12)
                        continue;
                    toRemove.Add(line);
                }
                else if (child is Ellipse ellipse)
                {
                    if (ellipse.Width == 10 && ellipse.StrokeThickness == 2)
                        continue;
                    toRemove.Add(ellipse);
                }
            }

            foreach (var element in toRemove)
            {
                BoardCanvas.Children.Remove(element);
            }

            if (_hoverIndicator != null)
            {
                BoardCanvas.Children.Remove(_hoverIndicator);
                _hoverIndicator = null;
            }

            if (_pulseIndicator != null)
            {
                BoardCanvas.Children.Remove(_pulseIndicator);
                _pulseIndicator = null;
            }

            var board = _viewModel.Board;
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    if (board[i, j] != ChessBoard.EMPTY)
                    {
                        DrawStone(i, j, board[i, j]);
                    }
                }
            }

            var lastMove = _viewModel.LastMove;
            if (lastMove.x >= 0 && lastMove.y >= 0 && !_viewModel.IsGameOver)
            {
                DrawPulseIndicator();
                Canvas.SetLeft(_pulseIndicator, PADDING + lastMove.x * CELL_SIZE - (_pulseIndicator?.Width ?? 0) / 2);
                Canvas.SetTop(_pulseIndicator, PADDING + lastMove.y * CELL_SIZE - (_pulseIndicator?.Height ?? 0) / 2);
                _pulseIndicator.Visibility = Visibility.Visible;
            }

            StatusText.Text = _viewModel.StatusText;
        }

        private int GetBaseElementCount()
        {
            int count = 0;
            foreach (var child in BoardCanvas.Children)
            {
                if (child is Line line && line.StrokeThickness == 1)
                    count++;
                else if (child is Ellipse ellipse && ellipse.Width == 10 && ellipse.StrokeThickness == 2)
                    count++;
            }
            return count;
        }

        private void DrawStone(int x, int y, int color)
        {
            double radius = STONE_SIZE / 2;
            
            if (color == ChessBoard.BLACK)
            {
                var outline = new Ellipse
                {
                    Width = STONE_SIZE + 2,
                    Height = STONE_SIZE + 2,
                    Fill = new SolidColorBrush(Color.FromRgb(0x1A, 0x25, 0x2F))
                };
                
                var mainStone = new Ellipse
                {
                    Width = STONE_SIZE,
                    Height = STONE_SIZE
                };
                
                var gradient = new RadialGradientBrush
                {
                    GradientOrigin = new Point(0.3, 0.3)
                };
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x5D, 0x6D, 0x7E), 0));
                gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x2C, 0x3E, 0x50), 1));
                mainStone.Fill = gradient;
                
                var highlight = new Ellipse
                {
                    Width = STONE_SIZE * 0.35,
                    Height = STONE_SIZE * 0.22,
                    Fill = new SolidColorBrush(Color.FromArgb(77, 255, 255, 255))
                };
                
                Canvas.SetLeft(outline, PADDING + x * CELL_SIZE - radius - 1);
                Canvas.SetTop(outline, PADDING + y * CELL_SIZE - radius - 1);
                Canvas.SetLeft(mainStone, PADDING + x * CELL_SIZE - radius);
                Canvas.SetTop(mainStone, PADDING + y * CELL_SIZE - radius);
                Canvas.SetLeft(highlight, PADDING + x * CELL_SIZE - STONE_SIZE * 0.175);
                Canvas.SetTop(highlight, PADDING + y * CELL_SIZE - STONE_SIZE * 0.32);
                
                BoardCanvas.Children.Add(outline);
                BoardCanvas.Children.Add(mainStone);
                BoardCanvas.Children.Add(highlight);
            }
            else
            {
                var outline = new Ellipse
                {
                    Width = STONE_SIZE + 2,
                    Height = STONE_SIZE + 2,
                    Fill = new SolidColorBrush(Color.FromRgb(0xD5, 0xDB, 0xDB))
                };
                
                var mainStone = new Ellipse
                {
                    Width = STONE_SIZE,
                    Height = STONE_SIZE,
                    Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0xFA, 0xFA))
                };
                
                var highlight = new Ellipse
                {
                    Width = 3,
                    Height = 3,
                    Fill = Brushes.White
                };
                
                Canvas.SetLeft(outline, PADDING + x * CELL_SIZE - radius - 1);
                Canvas.SetTop(outline, PADDING + y * CELL_SIZE - radius - 1);
                Canvas.SetLeft(mainStone, PADDING + x * CELL_SIZE - radius);
                Canvas.SetTop(mainStone, PADDING + y * CELL_SIZE - radius);
                Canvas.SetLeft(highlight, PADDING + x * CELL_SIZE - radius + 2);
                Canvas.SetTop(highlight, PADDING + y * CELL_SIZE - radius + 2);
                
                BoardCanvas.Children.Add(outline);
                BoardCanvas.Children.Add(mainStone);
                BoardCanvas.Children.Add(highlight);
            }
        }

        private void DrawWinningLine()
        {
            var winningLine = _viewModel.WinningLine;
            if (winningLine == null || winningLine.Count < 5)
                return;

            var startPos = winningLine[0];
            var endPos = winningLine[winningLine.Count - 1];

            double x1 = PADDING + startPos.x * CELL_SIZE;
            double y1 = PADDING + startPos.y * CELL_SIZE;
            double x2 = PADDING + endPos.x * CELL_SIZE;
            double y2 = PADDING + endPos.y * CELL_SIZE;

            var outerGlow = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0xB6, 0xC1)),
                StrokeThickness = 20,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false,
                Opacity = 0.5
            };
            BoardCanvas.Children.Add(outerGlow);

            var innerGlow = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = Brushes.White,
                StrokeThickness = 10,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false,
                Opacity = 0.8
            };
            BoardCanvas.Children.Add(innerGlow);

            var mainLine = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                StrokeThickness = 6,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };
            BoardCanvas.Children.Add(mainLine);

            foreach (var (wx, wy) in winningLine)
            {
                double stoneCenterX = PADDING + wx * CELL_SIZE;
                double stoneCenterY = PADDING + wy * CELL_SIZE;

                var glowOutline = new Ellipse
                {
                    Width = STONE_SIZE + 10,
                    Height = STONE_SIZE + 10,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0xB6, 0xC1)),
                    StrokeThickness = 4,
                    Fill = Brushes.Transparent,
                    IsHitTestVisible = false,
                    Opacity = 0.7
                };
                Canvas.SetLeft(glowOutline, stoneCenterX - (STONE_SIZE + 10) / 2);
                Canvas.SetTop(glowOutline, stoneCenterY - (STONE_SIZE + 10) / 2);
                BoardCanvas.Children.Add(glowOutline);

                var pinkOutline = new Ellipse
                {
                    Width = STONE_SIZE + 6,
                    Height = STONE_SIZE + 6,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                    StrokeThickness = 3,
                    Fill = Brushes.Transparent,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(pinkOutline, stoneCenterX - (STONE_SIZE + 6) / 2);
                Canvas.SetTop(pinkOutline, stoneCenterY - (STONE_SIZE + 6) / 2);
                BoardCanvas.Children.Add(pinkOutline);
            }

            var pulseAnimation = new DoubleAnimation
            {
                From = 0.6,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(800),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            mainLine.BeginAnimation(OpacityProperty, pulseAnimation);
            innerGlow.BeginAnimation(OpacityProperty, pulseAnimation);
            outerGlow.BeginAnimation(OpacityProperty, pulseAnimation);
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            var stonesToRemove = BoardCanvas.Children.Count - GetBaseElementCount();
            for (int i = 0; i < stonesToRemove; i++)
            {
                BoardCanvas.Children.RemoveAt(BoardCanvas.Children.Count - 1);
            }
            _hoverIndicator = null;
            if (_pulseIndicator != null)
            {
                BoardCanvas.Children.Remove(_pulseIndicator);
                _pulseIndicator = null;
            }
            _viewModel.Restart();
            UpdateBoardDisplay();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}