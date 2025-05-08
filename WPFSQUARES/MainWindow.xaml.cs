using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WPFSQUARES
{
    public partial class MainWindow : Window
    {
        private const int GridSize = 10; // размер поля (из скольки клеточек сторона)
        private const int CellSize = 50; // размер самой клеточки (вообще таблица клеток регулирует размер экрана)

        private readonly Brush Player1Brush = Brushes.Blue; //кисточки игрокам
        private readonly Brush Player2Brush = Brushes.Red;

        private int[,] gridOwners;  // двумерный массив , в котором хранятся владельцы клеток 0-никто, 1-синий,2-красный
        private bool isPlayer1Turn = true; //переменная которая регулирует очередность хода
        private bool gameOver; //игра все или еще нет (когда она false, больше нельзя будет жмать по клеткам таблицы)

        private List<(int row, int col)> winningCells = new(); //список координат клеточек, которые при победе окрашивать специально будем

        public MainWindow()
        {
            InitializeComponent();  // создает все, что описано в xaml
            InitializeGameGrid();  //настройки отображения сетки
            InitializeGame();   // сброс до базы игровых состояний
        }

        private void InitializeGameGrid()
        {
            GameGrid.RowDefinitions.Clear();  // очистка старой сетки если что-то было 
            GameGrid.ColumnDefinitions.Clear();
            GameGrid.Children.Clear();

            for (int i = 0; i < GridSize; i++) //добавляем 10 строк и 10 столбцов, каждый одинакового размера
            {
                GameGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CellSize) });
                GameGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CellSize) });
            }

            for (int row = 0; row < GridSize; row++) //создаем клеточки устраиваем в нужную строку и столбец
            {
                for (int col = 0; col < GridSize; col++)
                {
                    var cell = new Rectangle
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = 0.5,
                        Fill = Brushes.White
                    };

                    cell.MouseLeftButtonDown += Cell_MouseClick;
                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    Panel.SetZIndex(cell, 0); // на задний план
                    GameGrid.Children.Add(cell);
                }
            }
        }

        private void InitializeGame() //сбрасываем игровую матрицу, тайтл в окне и прочее
        {
            gridOwners = new int[GridSize, GridSize];
            isPlayer1Turn = true;
            gameOver = false;
            UpdateTitle();

            foreach (var child in GameGrid.Children) //покраска в начальный цвет
            {
                if (child is Rectangle rect)
                {
                    rect.Stroke = Brushes.Black;
                    rect.StrokeThickness=0.5;
                    rect.Fill = Brushes.White;
                    Panel.SetZIndex(rect, 0);
                }
            }
        }

        private void Cell_MouseClick(object sender, MouseButtonEventArgs e) //обработка хода
        {
            if (gameOver) return; // нельзя жмать, если игра - всё

            var cell = (Rectangle)sender;
            int row = Grid.GetRow(cell);
            int col = Grid.GetColumn(cell); // получаем координаты клетки, по которой кликали

            if (gridOwners[row, col] != 0) return; // если уже кто-то владеет, то нельзя кликать

            
            gridOwners[row, col] = isPlayer1Turn ? 1 : 2; //если очередь синего, то закинем 1, если красного, то 2
            cell.Fill = isPlayer1Turn ? Player1Brush : Player2Brush; //ну и красим соответственно
            Panel.SetZIndex(cell, 1); // поднимаем на верхний слой

            if (CheckForWin(row, col)) //проверяем, что там по квадрату и повернутому квадрату 
            {
                gameOver = true; //если попали, то всё
                HiglightWinCells(); //выделяем выигрышный квадратик
                MessageBox.Show($"Победил {(isPlayer1Turn ? "Синий" : "Красный")} игрок!",
                              "Игра окончена",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
                UpdateTitle(true);//победное сообщение где тайтл тогда
                return;
            }

            isPlayer1Turn = !isPlayer1Turn; //меняем очередь
            UpdateTitle(); //просто обновляем текст окна
        }

        private void HiglightWinCells() //подсвечивание
        {
            foreach (var(r,c) in winningCells)  //для каждой пары координат в листе
            {
                var cell = GetRectangleAt(r, c); //получаем наш объектик прямоугольник
                if (cell!=null)
                {
                    cell.Stroke = Brushes.Yellow;
                    cell.StrokeThickness = 3;
                    Panel.SetZIndex(cell, 2);
                }
            }
        }

        private Rectangle GetRectangleAt(int r, int c) 
        {
            foreach (UIElement element in GameGrid.Children) //находим по координатам прямоугольник просто
            {
                if (element is Rectangle rect &&
                    Grid.GetRow(rect) == r &&
                    Grid.GetColumn(rect) == c)
                {
                    return rect;
                }
            }

            return null;
        }

        private bool CheckForWin(int row, int col)
        {
            int currentPlayer = isPlayer1Turn ? 1 : 2; //узнали номер текущего игрока

            // проверка всех возможных размеров квадратов (радиус)
            for (int size = 1; size < GridSize; size++)
            {
                // обычные квадраты
                if (CheckSquare(row, col, size, currentPlayer, false)) return true;

                // ромбы (повернутые квадраты)
                if (CheckSquare(row, col, size, currentPlayer, true)) return true;
            }
            return false;
        }

        private bool CheckSquare(int row, int col, int size, int player, bool isDiamond)  
        {
            
            if (isDiamond)
            {
                // проверяем все возможные центры ромба радиуса size, в которых (row, col) может быть частью
                for (int centerRow = row - size; centerRow <= row + size; centerRow++)
                {
                    for (int centerCol = col - size; centerCol <= col + size; centerCol++)
                    {
                        List<(int, int)> currentCells = new();
                        bool match = true;
                        for (int dr = -size; dr <= size && match; dr++)
                        {
                            for (int dc = -size; dc <= size; dc++)
                            {
                                if (Math.Abs(dr) + Math.Abs(dc) > size) continue;

                                int r = centerRow + dr;
                                int c = centerCol + dc;

                                if (r < 0 || r >= GridSize || c < 0 || c >= GridSize || gridOwners[r, c] != player)
                                {
                                    match = false;
                                    break;
                                }
                                currentCells.Add((r, c));
                            }
                        }

                        if (match)
                        {
                            winningCells=currentCells;
                            return true;

                        }
                    }
                }
            }
            else
            {
                // проверяем все возможные верхние левые углы квадратов размера (size + 1),
                // в которых (row, col) может быть частью
                for (int r0 = row - size; r0 <= row; r0++)
                {
                    for (int c0 = col - size; c0 <= col; c0++)
                    {
                        if (r0 < 0 || c0 < 0 || r0 + size >= GridSize || c0 + size >= GridSize) //проверка не выходит ли за границы поля
                            continue;

                        List<(int, int)> currentCells = new(); //собираем грибы в виде клеточек для победы
                        bool match = true;
                        for (int dr = 0; dr <= size && match; dr++) 
                        {
                            for (int dc = 0; dc <= size; dc++)
                            {
                                int r = r0+dr;
                                int c = c0+dc;
                                if (gridOwners[r, c] != player)
                                {
                                    match = false;
                                    break;
                                }
                                currentCells.Add((r, c));
                            }
                        }

                        if (match) 
                        {
                            winningCells=currentCells;
                            return true; 
                        }

                    }
                }
            }

            return false;



        }

        private void UpdateTitle(bool gameEnded = false) //меняем заголовок в зависимости от состояния игры
        {
            Title = gameEnded
                ? $"Квадраты - Победил {(isPlayer1Turn ? "Синий" : "Красный")} игрок!"
                : $"Квадраты - Ход {(isPlayer1Turn ? "Синего" : "Красного")} игрока";
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e) //когда на кнопку жмаем, обновляет все состояние игры
        {
            InitializeGame();
        }
    }
}