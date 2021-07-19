using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

Console.WriteLine(File.ReadAllText("input.txt").Solve());

public static class Solver
{
    public static (int score, int nonsenseCharacterCount) Solve(this string inputString)
    {
        var cleansedInput = Regex.Replace(inputString, @"!(.){1}", string.Empty);
        Node node = new Root();
        foreach (var character in cleansedInput)
        {
            node = node.Process(character);
        }

        return (node.Score, node.NonsenseCharacterCount);
    }
}

public abstract class Node
{
    public abstract int Score { get; }
    public abstract int Depth { get; }
    public abstract int NonsenseCharacterCount { get; }
    public abstract Node Process(char character);
}

public class Root : Node
{
    private Node _child;

    public override int Score => _child.Score;
    public override int Depth => 0;
    public override int NonsenseCharacterCount => _child.NonsenseCharacterCount;

    public override Node Process(char character)
    {
        if (character is '{')
        {
            _child = new Group(this);
            return _child;
        }

        if (character is '<')
        {
            _child = new Nonsense(this);
            return _child;
        }

        return this;
    }
}

public class Group : Node
{
    private readonly List<Node> _children = new();

    private readonly Node _parent;

    public Group(Node parent)
    {
        _parent = parent;
    }

    public override int Score => Depth + _children.Sum(_ => _.Score);
    public override int Depth => _parent.Depth + 1;
    public override int NonsenseCharacterCount => _children.Sum(_ => _.NonsenseCharacterCount);
    public override Node Process(char character)
    {
        if (character is '{')
        {
            var group = new Group(this);
            _children.Add(group);
            return group;
        }

        if (character is '<')
        {
            var nonsense = new Nonsense(this);
            _children.Add(nonsense);
            return nonsense;
        }

        if (character is '}')
        {
            return _parent;
        }

        return this;
    }
}

public class Nonsense : Node
{
    private int _nonsenseCharacterCount;
    private readonly Node _parent;

    public Nonsense(Node parent)
    {
        _parent = parent;
    }

    public override int Score => 0;
    public override int Depth => 0;
    public override int NonsenseCharacterCount => _nonsenseCharacterCount;

    public override Node Process(char character)
    {
        if (character is '>')
        {
            return _parent;
        }

        _nonsenseCharacterCount++;
        return this;
    }
}

public static class Tests
{
    [Theory]
    [InlineData("{}", 1)]
    [InlineData("{{{}}}", 6)]
    [InlineData("{{},{}}", 5)]
    [InlineData("{{{},{},{{}}}}", 16)]
    [InlineData("{<a>,<a>,<a>,<a>}", 1)]
    [InlineData("{{<ab>},{<ab>},{<ab>},{<ab>}}", 9)]
    [InlineData("{{<!!>},{<!!>},{<!!>},{<!!>}}", 9)]
    public static void ValidateExamples(string input, int expectedScore) => input.Solve().score.ShouldBe(expectedScore);

    [Fact]
    public static void ValidateInputExample() => File.ReadAllText("input.txt").Solve().score.ShouldBe(13154);

    [Fact]
    public static void ValidateInputExample2() => File.ReadAllText("input.txt").Solve().nonsenseCharacterCount.ShouldBe(6369);
}
