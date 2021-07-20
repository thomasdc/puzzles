using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shouldly;
using Xunit;

public static class Solver
{
    public static IEnumerable<bool[,]> Iterate(this ExpansionRule[] rules)
    {
        var grid = ".#./..#/###".ParseGrid();
        while (true)
        {
            yield return grid;
            var subdividedGrid = grid.Subdivide();
            
            // Expand
            var expandedGrid = new bool[subdividedGrid.Size(), subdividedGrid.Size()][,];
            for (var y = 0; y < subdividedGrid.Size(); y++)
            {
                for (var x = 0; x < subdividedGrid.Size(); x++)
                {
                    var matchingRule = rules.First(_ => _.AppliesTo(subdividedGrid[y, x]));
                    expandedGrid[y, x] = matchingRule.To.Clone() as bool[,];
                }
            }

            // Stitch
            var innerSubGridSize = expandedGrid[0, 0].Size();
            var newSize = expandedGrid.Size() * innerSubGridSize;
            grid = new bool[newSize, newSize];

            for (var y = 0; y < expandedGrid.Size(); y++)
            {
                for (var x = 0; x < expandedGrid.Size(); x++)
                {
                    for (var yy = 0; yy < innerSubGridSize; yy++)
                    {
                        for (var xx = 0; xx < innerSubGridSize; xx++)
                        {
                            grid[y * innerSubGridSize + yy, x * innerSubGridSize + xx] = expandedGrid[y, x][yy, xx];
                        }
                    }
                }
            }
        }
    }

    public static T[,][,] Subdivide<T>(this T[,] grid)
    {
        var innerSubGridSize = grid.Size() % 2 == 0 ? 2 : 3;

        var subGridsSize = grid.Size() / innerSubGridSize;
        var subGrids = new T[subGridsSize, subGridsSize][,];

        for (var y = 0; y < subGridsSize; y++)
        {
            for (var x = 0; x < subGridsSize; x++)
            {
                var subGrid = new T[innerSubGridSize, innerSubGridSize];
                for (var yy = 0; yy < innerSubGridSize; yy++)
                {
                    for (var xx = 0; xx < innerSubGridSize; xx++)
                    {
                        subGrid[yy, xx] = grid[y * innerSubGridSize + yy, x * innerSubGridSize + xx];
                    }
                }

                subGrids[y, x] = subGrid;
            }
        }

        return subGrids;
    }
}

public record ExpansionRule(bool[,] From, bool[,] To)
{
    public bool AppliesTo(bool[,] subGrid)
    {
        if (From.Size() != subGrid.Size()) return false;
        var applies = false;
        for (var i = 0; i < 4; i++)
        {
            applies |= subGrid.Matches(From);

            subGrid.FlipVertically();
            applies |= subGrid.Matches(From);
            subGrid.FlipVertically();

            subGrid.FlipHorizontally();
            applies |= subGrid.Matches(From);
            subGrid.FlipHorizontally();

            subGrid.Rotate();
        }

        return applies;
    }
}

public static class UtilExtensions
{
    public static int PixelCount(this bool[,] grid) => grid.Cast<bool>().Count(_ => _);
    public static int Size<T>(this T[,] subGrid) => subGrid.GetLength(0);
    public static bool Matches(this bool[,] left, bool[,] right) => left.Cast<bool>().SequenceEqual(right.Cast<bool>());

    public static void Rotate<T>(this T[,] grid)
    {
        var size = grid.Size();
        for (var offset = 0; offset < size - 1; offset++)
        {
            var temp = grid[0, offset];
            grid[0, offset] = grid[size - 1 - offset, 0];
            grid[size - 1 - offset, 0] = grid[size - 1, size - 1 - offset];
            grid[size - 1, size - 1 - offset] = grid[offset, size - 1];
            grid[offset, size - 1] = temp;
        }
    }

    public static void FlipVertically<T>(this T[,] grid)
    {
        var size = grid.Size();
        for (var y = 0; y < size; y++)
        {
            var temp = grid[y, 0];
            grid[y, 0] = grid[y, size - 1];
            grid[y, size - 1] = temp;
        }
    }

    public static void FlipHorizontally<T>(this T[,] grid)
    {
        var size = grid.Size();
        for (var x = 0; x < size; x++)
        {
            var temp = grid[0, x];
            grid[0, x] = grid[size - 1, x];
            grid[size - 1, x] = temp;
        }
    }

    public static ExpansionRule[] Parse(this string[] input) => input.Select(_ => _.Parse()).ToArray();

    public static ExpansionRule Parse(this string input)
    {
        var split = input.Split(" => ");
        return new ExpansionRule(split[0].ParseGrid(), split[1].ParseGrid());
    }

    public static bool[,] ParseGrid(this string input)
    {
        var split = input.Split('/');
        var size = split.Length;
        var grid = new bool[size, size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                grid[y, x] = split[y][x] == '#';
            }
        }

        return grid;
    }
}

public static class Tests
{
    [Theory]
    [InlineData(5, 171)]
    [InlineData(18, 2498142)]
    public static void ValidateInputExamples(int numberOfIterations, int expectedPixelCount) => 
        File.ReadAllLines("input.txt").Parse().Iterate().Skip(numberOfIterations).Take(1).Single().PixelCount().ShouldBe(expectedPixelCount);

    [Fact]
    public static void ValidateGridParsing2By2() => "../.#".ParseGrid()
        .ShouldBe(new[,] {{false, false}, {false, true}});

    [Fact]
    public static void ValidateGridParsing3By3() => ".#./..#/###".ParseGrid()
        .ShouldBe(new[,] {{false, true, false}, {false, false, true}, {true, true, true}});

    [Fact]
    public static void ValidateRotation2By2()
    {
        var grid = new[,] {{1, 2}, {3, 4}};
        grid.Rotate();
        grid.ShouldBe(new[,] {{3, 1}, {4, 2}});
    }

    [Fact]
    public static void ValidateRotation3By3()
    {
        var grid = new[,] {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}};
        grid.Rotate();
        grid.ShouldBe(new[,] {{7, 4, 1}, {8, 5, 2}, {9, 6, 3}});
    }

    [Fact]
    public static void ValidateFlipVertically2By2()
    {
        var grid = new[,] {{1, 2}, {3, 4}};
        grid.FlipVertically();
        grid.ShouldBe(new[,] {{2, 1}, {4, 3}});
    }

    [Fact]
    public static void ValidateFlipVertically3By3()
    {
        var grid = new[,] {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}};
        grid.FlipVertically();
        grid.ShouldBe(new[,] {{3, 2, 1}, {6, 5, 4}, {9, 8, 7}});
    }

    [Fact]
    public static void ValidateFlipHorizontally2By2()
    {
        var grid = new[,] {{1, 2}, {3, 4}};
        grid.FlipHorizontally();
        grid.ShouldBe(new[,] {{3, 4}, {1, 2}});
    }

    [Fact]
    public static void ValidateFlipHorizontally3By3()
    {
        var grid = new[,] {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}};
        grid.FlipHorizontally();
        grid.ShouldBe(new[,] {{7, 8, 9}, {4, 5, 6}, {1, 2, 3}});
    }

    [Theory]
    [InlineData(".#./..#/### => #..#/..../..../#..#", ".#./..#/###", true)]
    [InlineData(".#./..#/### => #..#/..../..../#..#", "#../#.#/##.", true)]
    [InlineData(".#./..#/### => #..#/..../..../#..#", "###/#../.#.", true)]
    [InlineData(".#./..#/### => #..#/..../..../#..#", ".##/#.#/..#", true)]
    [InlineData(".#./..#/### => #..#/..../..../#..#", "###/..#/.#.", true)]
    [InlineData(".#./..#/### => #..#/..../..../#..#", ".#./#../###", true)]
    public static void ValidateExpansionRuleApplication(string ruleString, string gridString, bool shouldApply) =>
        ruleString.Parse().AppliesTo(gridString.ParseGrid()).ShouldBe(shouldApply);

    [Fact]
    public static void ValidateSubdivision()
    {
        "#..#/..../..../#..#".ParseGrid().Subdivide().ShouldSatisfyAllConditions(
            _ => _.Length.ShouldBe(4),
            _ => _[0, 0].ShouldBe(new[,] {{true, false}, {false, false}}),
            _ => _[0, 1].ShouldBe(new[,] {{false, true}, {false, false}}),
            _ => _[1, 0].ShouldBe(new[,] {{false, false}, {true, false}}),
            _ => _[1, 1].ShouldBe(new[,] {{false, false}, {false, true}}));
    }
}
