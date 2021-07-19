using System;
using System.IO;
using System.Linq;
using Shouldly;
using Xunit;

Console.WriteLine(File.ReadAllText("input.txt").CalculateChecksum());

public static class Solver
{
    public static double CalculateChecksum(this string inputString)
    {
        var numbers = inputString.Select(_ => int.Parse(_.ToString())).ToArray();
        double checksum = 0;
        for (var i=0; i<numbers.Length; i++)
        {
            checksum += numbers[i] == numbers[(i + 1) % numbers.Length] ? numbers[i] : 0;
        }
        
        return checksum;
    }
}

public static class Tests
{
    [Theory]
    [InlineData("1122", 3)]
    [InlineData("1111", 4)]
    [InlineData("1234", 0)]
    [InlineData("91212129", 9)]
    public static void ValidateChecksumExamples(string input, double expectedSum) => input.CalculateChecksum().ShouldBe(expectedSum);

    [Fact]
    public static void ValidateInputExample() => File.ReadAllText("input.txt").CalculateChecksum().ShouldBe(1171);
}
