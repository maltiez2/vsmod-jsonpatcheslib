namespace JsonPatchLib.Expressions.Nodes;

public sealed class Ternary<TOutput, TIntermediate, TInput> : INode<TOutput, TIntermediate, TInput>
{
    private readonly INode<TIntermediate, TIntermediate, TInput> _firstOperand;
    private readonly INode<TIntermediate, TIntermediate, TInput> _secondOperand;
    private readonly INode<TIntermediate, TIntermediate, TInput> _thirdOperand;
    private readonly Func<TIntermediate, TIntermediate, TIntermediate, TOutput> _operation;

    public Ternary(INode<TIntermediate, TIntermediate, TInput> firstOperand, INode<TIntermediate, TIntermediate, TInput> secondOperand, INode<TIntermediate, TIntermediate, TInput> thirdOperand, Func<TIntermediate, TIntermediate, TIntermediate, TOutput> operation)
    {
        _firstOperand = firstOperand;
        _secondOperand = secondOperand;
        _thirdOperand = thirdOperand;
        _operation = operation;
    }

    public TOutput Evaluate(IContext<TIntermediate, TInput> context) => _operation(_firstOperand.Evaluate(context), _secondOperand.Evaluate(context), _thirdOperand.Evaluate(context));
}

public sealed class Binary<TOutput, TIntermediate, TInput> : INode<TOutput, TIntermediate, TInput>
{
    private readonly INode<TIntermediate, TIntermediate, TInput> _leftOperand;
    private readonly INode<TIntermediate, TIntermediate, TInput> _rightOperand;
    private readonly Func<TIntermediate, TIntermediate, TOutput> _operation;

    public Binary(INode<TIntermediate, TIntermediate, TInput> leftOperand, INode<TIntermediate, TIntermediate, TInput> rightOperand, Func<TIntermediate, TIntermediate, TOutput> operation)
    {
        _leftOperand = leftOperand;
        _rightOperand = rightOperand;
        _operation = operation;
    }

    public TOutput Evaluate(IContext<TIntermediate, TInput> context) => _operation(_leftOperand.Evaluate(context), _rightOperand.Evaluate(context));
}

public sealed class FunctionCall<TOutput, TInput> : INode<TOutput, TOutput, TInput>
{
    private readonly INode<TInput, TOutput, TInput>[] _arguments;
    private readonly string _functionName;

    public FunctionCall(string functionName, INode<TInput, TOutput, TInput>[] arguments)
    {
        _functionName = functionName;
        _arguments = arguments;
    }

    public TOutput Evaluate(IContext<TOutput, TInput> context) => context.Resolve(_functionName, _arguments.Select(argument => argument.Evaluate(context)).ToArray());
}

public readonly struct Value<TOutput, TIntermediate, TInput> : INode<TOutput, TIntermediate, TInput>
{
    private readonly TOutput _value;

    public Value(TOutput number)
    {
        _value = number;
    }

    public TOutput Evaluate(IContext<TIntermediate, TInput> context) => _value;
}

public sealed class Unary<TOutput, TIntermediate, TInput> : INode<TOutput, TIntermediate, TInput>
{
    private readonly INode<TIntermediate, TIntermediate, TInput> _operand;
    private readonly Func<TIntermediate, TOutput> _operation;

    public Unary(INode<TIntermediate, TIntermediate, TInput> operand, Func<TIntermediate, TOutput> operation)
    {
        _operand = operand;
        _operation = operation;
    }

    public TOutput Evaluate(IContext<TIntermediate, TInput> context) => _operation.Invoke(_operand.Evaluate(context));
}

public sealed class Variable<TOutput, TInput> : INode<TOutput, TOutput, TInput>
{
    private readonly string _variableName;

    public Variable(string variableName)
    {
        _variableName = variableName;
    }

    public TOutput Evaluate(IContext<TOutput, TInput> context) => context.Resolve(_variableName);
}
