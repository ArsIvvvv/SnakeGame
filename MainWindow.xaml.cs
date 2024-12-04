using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;


namespace SnakeGame
{



    public partial class MainWindow : Window
    {
        private const int SnakeSquareSize = 30;
        private readonly SolidColorBrush _snakeColor = Brushes.Green;
        private bool isPaused = false;
        private double currentInterval = 200;
        private bool isThroughWallsMode = false;

        private List<HighScore> highScores = new List<HighScore>();

        private enum Direction
        {
            Left, Up, Right, Down
        }

        private Direction _direction = Direction.Right;
        private const int TimerInterval = 200;

        private DispatcherTimer _timer;
        private Rectangle _snakeHead;
        private Point _foodPosition;
        private int _currentFoodType;


        private static readonly Random randomPositionFood = new Random();
        private List<Rectangle> _snake = new List<Rectangle>();
        private int _score = 0;

        private readonly string[] _foodImages = {
            "C:\\Users\\ivlev\\OneDrive\\Рабочий стол\\ИТИП\\SnakeGame\\Image\\apple.png",
            "C:\\Users\\ivlev\\OneDrive\\Рабочий стол\\ИТИП\\SnakeGame\\Image\\food.png",
            "C:\\Users\\ivlev\\OneDrive\\Рабочий стол\\ИТИП\\SnakeGame\\Image\\vinograd.png"
        };

        private readonly int[] _foodPoints = { 1, 3, 5 };
        

        public MainWindow()
        {
            InitializeComponent();

            LoadHighScores();
            DrawBorders(); // Рисуем границы при загрузке окна
        }

        private void InitialGame()
        {
            // Создаем голову змеи
            _snakeHead = CreateSnakeSegment(new Point(5, 5));
            _snake.Clear(); // Очищаем змею перед добавлением новых сегментов
            _snake.Add(_snakeHead); // Добавляем голову змеи в список
            GameCanvas.Children.Add(_snakeHead); // Добавляем голову в Canvas

            // Перезапускаем еду
            PlaceFood();

            // Инициализируем таймер
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(TimerInterval);
            _timer.Tick += Timer_Tick;
            _timer.Start();


        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Point newHeadPosition = CalcuteNewHeadPosition();

            // Проверяем, съела ли змейка еду
            if (newHeadPosition == _foodPosition)
            {
                EatFood();
                PlaceFood();
            }

            // Логика в зависимости от режима
            if (isThroughWallsMode)
            {
                // Проверка на выход за границы (сквозное прохождение)
                if (newHeadPosition.X < 0)
                {
                    newHeadPosition.X = (int)(GameCanvas.ActualWidth / SnakeSquareSize) - 1; // Переходит с правой стороны
                }
                else if (newHeadPosition.X >= GameCanvas.ActualWidth / SnakeSquareSize)
                {
                    newHeadPosition.X = 0; // Переходит с левой стороны
                }

                if (newHeadPosition.Y < 0)
                {
                    newHeadPosition.Y = (int)(GameCanvas.ActualHeight / SnakeSquareSize) - 1; // Переходит с нижней стороны
                }
                else if (newHeadPosition.Y >= GameCanvas.ActualHeight / SnakeSquareSize)
                {
                    newHeadPosition.Y = 0; // Переходит с верхней стороны
                }
            }
            else
            {
                // Проверка на выход за границы (обычное поведение)
                if (newHeadPosition.X < 0 || newHeadPosition.X >= GameCanvas.ActualWidth / SnakeSquareSize ||
                    newHeadPosition.Y < 0 || newHeadPosition.Y >= GameCanvas.ActualHeight / SnakeSquareSize)
                {
                    EndGame(); // Если выходит за границы, игра заканчивается
                    return;
                }
            }

            // Проверяем столкновение со своим телом
            if (_snake.Count >= 4)
            {
                for (int i = 0; i < _snake.Count; i++)
                {
                    Point currentPos = new Point(Canvas.GetLeft(_snake[i]), Canvas.GetTop(_snake[i]));
                    for (int j = i + 1; j < _snake.Count; j++)
                    {
                        Point nextPos = new Point(Canvas.GetLeft(_snake[j]), Canvas.GetTop(_snake[j]));
                        if (currentPos == nextPos)
                        {
                            EndGame();
                            return;
                        }
                    }
                }
            }

            // Перемещение хвоста
            for (int i = _snake.Count - 1; i > 0; i--)
            {
                Canvas.SetLeft(_snake[i], Canvas.GetLeft(_snake[i - 1]));
                Canvas.SetTop(_snake[i], Canvas.GetTop(_snake[i - 1]));
            }



            // Перемещаем голову змеи
            Canvas.SetLeft(_snakeHead, newHeadPosition.X * SnakeSquareSize);
            Canvas.SetTop(_snakeHead, newHeadPosition.Y * SnakeSquareSize);
        }

        private void EndGame()
        {
            _timer.Stop();


            MessageBox.Show($"Игра окончена! Твой счёт: {_score}");
            // Удаляем все объекты с игрового поля, включая еду
            GameCanvas.Children.Clear();

            // Сохраняем рекорд (не передаем _score, так как метод SaveCurrentScore() сам получает его)
            SaveCurrentScore();

            // Обновляем таблицу рекордов
            ShowHighScores();

        }

        private void EatFood()
        {
            _score += _foodPoints[_currentFoodType];
            ScoreTextBlock.Text = "Счёт: " + _score;

            // Удаляем старое изображение еды
            GameCanvas.Children.Remove(GameCanvas.Children.OfType<Image>().FirstOrDefault());

            // Создаем новый сегмент змеи
            Rectangle newSnake = CreateSnakeSegment(_foodPosition);
            _snake.Add(newSnake);
            GameCanvas.Children.Add(newSnake);


            // Увеличиваем скорость игры (уменьшаем интервал)
            if (currentInterval > 50) // Устанавливаем минимальный интервал
            {
                // Уменьшаем интервал, чтобы ускорить игру
                currentInterval = Math.Max(currentInterval - 10, 50); // Минимальный интервал 50
                _timer.Interval = TimeSpan.FromMilliseconds(currentInterval);
            }
        }

        private Point CalcuteNewHeadPosition()
        {
            double left = Canvas.GetLeft(_snakeHead) / SnakeSquareSize;
            double top = Canvas.GetTop(_snakeHead) / SnakeSquareSize;

            Point headCurrentPosition = new Point(left, top);
            Point newHeadPosition = new Point();

            switch (_direction)
            {
                case Direction.Left:
                    newHeadPosition = new Point(headCurrentPosition.X - 1, headCurrentPosition.Y);
                    break;
                case Direction.Right:
                    newHeadPosition = new Point(headCurrentPosition.X + 1, headCurrentPosition.Y);
                    break;
                case Direction.Up:
                    newHeadPosition = new Point(headCurrentPosition.X, headCurrentPosition.Y - 1);
                    break;
                case Direction.Down:
                    newHeadPosition = new Point(headCurrentPosition.X, headCurrentPosition.Y + 1);
                    break;
            }

            return newHeadPosition;
        }

        private void PlaceFood()
        {
            int maxX = (int)(GameCanvas.ActualWidth / SnakeSquareSize);
            int maxY = (int)(GameCanvas.ActualHeight / SnakeSquareSize);

            int foodX = randomPositionFood.Next(0, maxX);
            int foodY = randomPositionFood.Next(0, maxY);

            _foodPosition = new Point(foodX, foodY);
            _currentFoodType = randomPositionFood.Next(0, _foodImages.Length);

            Image foodImage = new Image
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Source = new BitmapImage(new Uri(_foodImages[_currentFoodType]))
            };

            Canvas.SetLeft(foodImage, foodX * SnakeSquareSize);
            Canvas.SetTop(foodImage, foodY * SnakeSquareSize);

            GameCanvas.Children.Add(foodImage);
        }



        private Rectangle CreateSnakeSegment(Point position)
        {
            Rectangle rectangle = new Rectangle
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = _snakeColor
            };

            Canvas.SetLeft(rectangle, position.X * SnakeSquareSize);
            Canvas.SetTop(rectangle, position.Y * SnakeSquareSize);
            return rectangle;
        }




        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    if (_direction != Direction.Down)
                        _direction = Direction.Up;
                    break;
                case Key.Down:
                    if (_direction != Direction.Up)
                        _direction = Direction.Down;
                    break;
                case Key.Left:
                    if (_direction != Direction.Right)
                        _direction = Direction.Left;
                    break;
                case Key.Right:
                    if (_direction != Direction.Left)
                        _direction = Direction.Right;
                    break;
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPaused)
            {
                // Возобновляем игру
                isPaused = false;
                PauseButton.Content = "Пауза";
                _timer.Start(); // Запускаем таймер

                // Активируем движение змейки
                _direction = _direction; // Не меняем направление, просто активируем таймер снова

                // Разрешаем всем игровым элементам снова обновляться (таймер снова работает)
                foreach (var element in GameCanvas.Children.OfType<UIElement>())
                {
                    element.IsEnabled = true;
                }
            }
            else
            {
                // Останавливаем игру
                isPaused = true;
                PauseButton.Content = "Продолжить";
                _timer.Stop(); // Останавливаем таймер

                // Запрещаем всем игровым элементам обновляться (замораживаем состояние)
                foreach (var element in GameCanvas.Children.OfType<UIElement>())
                {
                    element.IsEnabled = false;
                }
            }
        }



        private void DrawBorders()
        {
            // Очищаем все элементы с Canvas перед рисованием границ
            GameCanvas.Children.Clear();

            // Создаем границы
            Line topBorder = new Line { X1 = 0, Y1 = 0, X2 = 600, Y2 = 0, Stroke = Brushes.Black, StrokeThickness = 2 };
            Line leftBorder = new Line { X1 = 0, Y1 = 0, X2 = 0, Y2 = 400, Stroke = Brushes.Black, StrokeThickness = 2 };
            Line rightBorder = new Line { X1 = 600, Y1 = 0, X2 = 600, Y2 = 400, Stroke = Brushes.Black, StrokeThickness = 2 };
            Line bottomBorder = new Line { X1 = 0, Y1 = 400, X2 = 600, Y2 = 400, Stroke = Brushes.Black, StrokeThickness = 2 };

            GameCanvas.Children.Add(topBorder);
            GameCanvas.Children.Add(leftBorder);
            GameCanvas.Children.Add(rightBorder);
            GameCanvas.Children.Add(bottomBorder);
        }


        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем главное меню и показываем игровое поле
            MainMenuPanel.Visibility = Visibility.Collapsed;
            GameCanvas.Visibility = Visibility.Visible;
            ControlPanel.Visibility = Visibility.Visible;

            LoadHighScores();


            // Сброс игры
            GameCanvas.Children.Clear();
            _snake.Clear();
            _score = 0;
            ScoreTextBlock.Text = "Счёт: 0";

            // Рисуем границы
            DrawBorders();

            // Инициализация новой игры
            InitialGame();

            // Сброс скорости
            currentInterval = 200;  // Сбрасываем скорость на начальное значение
            _timer.Interval = TimeSpan.FromMilliseconds(currentInterval);
        }



        

        private void ShowControls_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed; // Скрываем главное меню
            ControlsPanel.Visibility = Visibility.Visible; // Показываем панель управления
        }

        private void ShowCreatorInfo_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed; // Скрываем главное меню
            CreatorInfoPanel.Visibility = Visibility.Visible; // Показываем информацию о создателе
        }


        private void BackToMainMenu_Click(object sender, RoutedEventArgs e)
        {
            // Скрыть панель с таблицей рекордов
            HighScoresPanel.Visibility = Visibility.Collapsed;

            // Показать главное меню
            MainMenuPanel.Visibility = Visibility.Visible;
            ControlsPanel.Visibility= Visibility.Collapsed;
            CreatorInfoPanel.Visibility= Visibility.Collapsed;
        }




        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // Показываем предупреждающее сообщение
            MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите выйти в меню? Все несохраненные данные будут потеряны.",
                "Предупреждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            // Если пользователь нажал "Yes"
            if (result == MessageBoxResult.Yes)
            {
                // Останавливаем таймер, если игра в процессе
                _timer.Stop();

                // Очищаем игровое поле от всех элементов
                GameCanvas.Children.Clear();

                // Скрываем игровое поле
                GameCanvas.Visibility = Visibility.Collapsed;

                // Показываем главное меню
                MainMenuPanel.Visibility = Visibility.Visible;

                // Также скрываем панель управления, если она была видна
                ControlPanel.Visibility = Visibility.Collapsed;

                // Сбрасываем все элементы змеи и еды (обнуляем состояние игры)
                _snake.Clear();
                _score = 0;
                ScoreTextBlock.Text = "Score: 0";
            }
            // Если пользователь выбрал "No", то ничего не происходит, и пользователь остается в игре
        }

        private void ShowHighScores_Click(object sender, RoutedEventArgs e)
        {
            ShowHighScores();
            
        }


        private void SaveHighScore(string playerName, int score)
        {
            string filePath = "highscores.txt";

            // Проверяем, существует ли файл
            if (!File.Exists(filePath))
            {
                // Если файла нет, создаем новый и записываем результат
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine($"{playerName}:{score}");
                }
            }
            else
            {
                // Если файл существует, добавляем новый рекорд
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine($"{playerName}:{score}");
                }
            }
        }

        
        private void LoadHighScores()
        {
            string filePath = "highscores.txt";
            if (File.Exists(filePath))
            {
                // Очищаем текущий список
                highScores.Clear();

                // Читаем записи из файла
                foreach (var line in File.ReadLines(filePath))
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        string playerName = parts[0];
                        if (int.TryParse(parts[1], out int score))
                        {
                            highScores.Add(new HighScore { PlayerName = playerName, Score = score });
                        }
                    }
                }

                // Сортируем таблицу рекордов по убыванию
                highScores = highScores.OrderByDescending(h => h.Score).ToList();
            }
        }


        private void ShowHighScores()
        {
            string filePath = "highscores.txt";
            if (File.Exists(filePath))
            {
                // Очищаем текущий список рекордов
                List<HighScore> highScores = new List<HighScore>();

                // Чтение строк из файла и создание списка рекордов
                foreach (var line in File.ReadLines(filePath))
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        var playerName = parts[0];
                        var score = int.Parse(parts[1]);
                        highScores.Add(new HighScore { PlayerName = playerName, Score = score });
                    }
                }

                // Сортируем рекорды по убыванию очков
                var topScores = highScores.OrderByDescending(s => s.Score).Take(5).ToList();

                // Отображаем только первые 5 рекордов
                HighScoresList.ItemsSource = topScores;
            }

            // Показываем таблицу рекордов
            HighScoresPanel.Visibility = Visibility.Visible;

            // Сдвигаем таблицу немного влево
            HighScoresPanel.Margin = new Thickness(10, 0, 0, 0); // В левую сторону

            GameCanvas.Visibility = Visibility.Collapsed;
            ControlPanel.Visibility = Visibility.Collapsed;
        }

        private void SaveCurrentScore()
        {
            string playerName = "Игрок ";
            int score = _score; 

            // Сохраняем результат в файл
            SaveHighScore(playerName, score);

            LoadHighScores();
        }

        private void HighScoresButton_Click(object sender, RoutedEventArgs e)
        {
            // Загружаем рекорды
            LoadHighScores();

        }

        private void ExitGame_Click(object sender, RoutedEventArgs e)
        {
            // Закрывает игру при нажатии на кнопку
            Application.Current.Shutdown();
        }


        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            // Переключение режима
            isThroughWallsMode = !isThroughWallsMode;

            // Обновляем текст на кнопке в зависимости от выбранного режима
            ModeButton.Content = isThroughWallsMode ? "Режим: Сквозь стены" : "Режим: Обычный";
        }



    }

    public class HighScore
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
    }
}
