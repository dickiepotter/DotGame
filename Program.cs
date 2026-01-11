using System;
using System.Threading;

namespace GameOfLife
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Conway's Game of Life");
            Console.WriteLine("====================\n");

            var game = new GameOfLife(40, 20);
            game.Run();
        }
    }

    class GameOfLife
    {
        private bool[,] grid;
        private bool[,] nextGrid;
        private int width;
        private int height;
        private int generation;

        public GameOfLife(int width, int height)
        {
            this.width = width;
            this.height = height;
            grid = new bool[width, height];
            nextGrid = new bool[width, height];
            generation = 0;

            InitializeRandom();
        }

        private void InitializeRandom()
        {
            var random = new Random();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    grid[x, y] = random.Next(100) < 30; // 30% chance of being alive
                }
            }
        }

        public void Run()
        {
            Console.CursorVisible = false;

            while (true)
            {
                Display();
                Update();
                generation++;
                Thread.Sleep(100); // 100ms delay between generations

                // Exit if user presses a key
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    break;
                }
            }

            Console.CursorVisible = true;
            Console.WriteLine("\nGame ended. Press any key to exit...");
            Console.ReadKey();
        }

        private void Display()
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"Generation: {generation}");
            Console.WriteLine("Press any key to exit\n");

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Console.Write(grid[x, y] ? "██" : "  ");
                }
                Console.WriteLine();
            }
        }

        private void Update()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int neighbors = CountNeighbors(x, y);

                    if (grid[x, y])
                    {
                        // Cell is alive
                        nextGrid[x, y] = neighbors == 2 || neighbors == 3;
                    }
                    else
                    {
                        // Cell is dead
                        nextGrid[x, y] = neighbors == 3;
                    }
                }
            }

            // Swap grids
            var temp = grid;
            grid = nextGrid;
            nextGrid = temp;
        }

        private int CountNeighbors(int x, int y)
        {
            int count = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = (x + dx + width) % width;   // Wrap around horizontally
                    int ny = (y + dy + height) % height; // Wrap around vertically

                    if (grid[nx, ny])
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
