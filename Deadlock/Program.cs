using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shouldly;
using Xunit;

public static class Solver
{
    public static double CalculateEnqueueCount(this Instruction[] instructions)
    {
        var computer0 = new Computer(0, instructions);
        var computer1 = new Computer(1, instructions);
        computer0.QueueOtherComputer = computer1.Queue;
        computer1.QueueOtherComputer = computer0.Queue;

        while (computer0.ExecutionState == ExecutionState.Running || computer1.ExecutionState == ExecutionState.Running)
        {
            computer0.Tick();
            computer1.Tick();
        }

        return computer1.EnqueueCount;
    }
}

public class Computer
{
    public long Id { get; }
    public Queue<long> Queue { get; }
    public Queue<long> QueueOtherComputer;
    public int EnqueueCount;
    public readonly IDictionary<string, long> Registers;
    public long InstructionPointer;
    public readonly Instruction[] Instructions;
    public ExecutionState ExecutionState;

    public Computer(long id, Instruction[] instructions)
    {
        Instructions = instructions;
        Id = id;
        Registers = new Dictionary<string, long>
        {
            {"p", id}
        };

        Queue = new Queue<long>();
    }

    public void Tick()
    {
        Instructions[InstructionPointer].Execute(this);
        if (InstructionPointer > Instructions.Length - 1)
        {
            ExecutionState = ExecutionState.Completed;
        }
    }

    public long GetValue(string registerOrValue)
    {
        if (long.TryParse(registerOrValue, out var value))
        {
            return value;
        }

        return Registers.TryGetValue(registerOrValue!, out var value2) ? value2 : 0;
    }
}

public enum ExecutionState
{
    Running,
    WaitingForValueOnQueue,
    Completed
}

public abstract record Instruction
{
    public abstract void Execute(Computer computer);
}

public record Set(string Register, string Y) : Instruction
{
    public override void Execute(Computer computer)
    {
        computer.Registers[Register] = computer.GetValue(Y);
        computer.InstructionPointer++;
    }
}

public record Add(string Register, string Y) : Instruction
{
    public override void Execute(Computer computer)
    {
        if (computer.Registers.ContainsKey(Register))
        {
            computer.Registers[Register] += computer.GetValue(Y);
        }
        else
        {
            computer.Registers[Register] = computer.GetValue(Y);
        }

        computer.InstructionPointer++;
    }
}

public record Sub(string Register, string Y) : Instruction
{
    public override void Execute(Computer computer)
    {
        if (computer.Registers.ContainsKey(Register))
        {
            computer.Registers[Register] -= computer.GetValue(Y);
        }
        else
        {
            computer.Registers[Register] = computer.GetValue(Y);
        }

        computer.InstructionPointer++;
    }
}

public record Mul(string RegisterX, long Value) : Instruction
{
    public override void Execute(Computer computer)
    {
        var xValue = computer.Registers.ContainsKey(RegisterX) ? computer.Registers[RegisterX] : 0;
        computer.Registers[RegisterX] = xValue * Value;
        computer.InstructionPointer++;
    }
}

public record Mod(string RegisterX, string Y) : Instruction
{
    public override void Execute(Computer computer)
    {
        var xValue = computer.Registers.ContainsKey(RegisterX) ? computer.Registers[RegisterX] : 0;
        var yValue = computer.GetValue(Y);
        computer.Registers[RegisterX] = xValue % yValue;
        computer.InstructionPointer++;
    }
}

public record Jgz(string X, string Y) : Instruction
{
    public override void Execute(Computer computer)
    {
        var xValue = computer.GetValue(X);
        var yValue = computer.GetValue(Y);
        if (xValue > 0)
        {
            computer.InstructionPointer += yValue;
        }
        else
        {
            computer.InstructionPointer++;
        }
    }
}

public record Snd(string X) : Instruction
{
    public override void Execute(Computer computer)
    {
        computer.QueueOtherComputer.Enqueue(computer.GetValue(X));
        computer.EnqueueCount++;
        computer.InstructionPointer++;
    }
}

public record Rcv(string Register) : Instruction
{
    public override void Execute(Computer computer)
    {
        if (computer.Queue.TryDequeue(out var value))
        {
            computer.Registers[Register] = value;
            computer.InstructionPointer++;
            computer.ExecutionState = ExecutionState.Running;
        }
        else
        {
            computer.ExecutionState = ExecutionState.WaitingForValueOnQueue;
        }
    }
}

public static class Parser
{
    public static Instruction Parse(this string input) =>
        input[..3] switch
        {
            "set" => new Set(input[4..5], input[6..]),
            "add" => new Add(input[4..5], input[6..]),
            "sub" => new Sub(input[4..5], input[6..]),
            "mul" => new Mul(input[4..5], long.Parse(input[6..])),
            "mod" => new Mod(input[4..5], input[6..]),
            "jgz" => new Jgz(input[4..5], input[6..]),
            "snd" => new Snd(input[4..]),
            "rcv" => new Rcv(input[4..]),
            _ => throw new ArgumentOutOfRangeException(nameof(input))
        };

    public static Instruction[] Parse(this string[] input) => input.Select(_ => _.Parse()).ToArray();
}

public static class Tests
{
    [Fact]
    public static void ValidateInputExample() => File.ReadAllLines("input.txt").Parse().CalculateEnqueueCount().ShouldBe(8001);
}
