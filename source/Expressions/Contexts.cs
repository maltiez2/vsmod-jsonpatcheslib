using System.Reflection;
using Vintagestory.API.Common;

namespace JsonPatchLib.Expressions;

public sealed class CombinedContext<TResult, TArguments> : IContext<TResult, TArguments>
{
    private readonly IEnumerable<IContext<TResult, TArguments>> _contexts;

    public CombinedContext(IEnumerable<IContext<TResult, TArguments>> contexts)
    {
        _contexts = contexts;
    }

    public bool Resolvable(string name)
    {
        foreach (IContext<TResult, TArguments> context in _contexts)
        {
            if (context.Resolvable(name)) return true;
        }

        return false;
    }
    public TResult Resolve(string name, params TArguments[] arguments)
    {
        foreach (IContext<TResult, TArguments> context in _contexts)
        {
            if (context.Resolvable(name)) return context.Resolve(name, arguments);
        }

        throw new InvalidDataException($"Unresolvable: '{name}'");
    }
}

public sealed class MathContext : IContext<float, float>
{
    private const float _epsilon = 1E-15f;

    public MathContext()
    {
    }

    public bool Resolvable(string name)
    {
        return name switch
        {
            "pi" => true,
            "e" => true,
            "sin" => true,
            "cos" => true,
            "abs" => true,
            "sqrt" => true,
            "ceiling" => true,
            "floor" => true,
            "exp" => true,
            "log" => true,
            "round" => true,
            "sign" => true,
            "clamp" => true,
            "max" => true,
            "min" => true,
            "greater" => true,
            "lesser" => true,
            "equal" => true,
            _ => false
        };
    }

    public float Resolve(string name, params float[] arguments)
    {
        return name switch
        {
            "pi" => MathF.PI,
            "e" => MathF.E,
            "sin" => MathF.Sin(arguments[0]),
            "cos" => MathF.Cos(arguments[0]),
            "abs" => MathF.Abs(arguments[0]),
            "sqrt" => MathF.Sqrt(arguments[0]),
            "ceiling" => MathF.Ceiling(arguments[0]),
            "floor" => MathF.Floor(arguments[0]),
            "exp" => MathF.Exp(arguments[0]),
            "log" => MathF.Log(arguments[0]),
            "round" => MathF.Round(arguments[0]),
            "sign" => MathF.Sign(arguments[0]),
            "clamp" => Math.Clamp(arguments[0], arguments[1], arguments[2]),
            "max" => MathF.Max(arguments[0], arguments[1]),
            "min" => MathF.Min(arguments[0], arguments[1]),
            "greater" => arguments[0] > arguments[1] ? arguments[2] : arguments[3],
            "lesser" => arguments[0] < arguments[1] ? arguments[2] : arguments[3],
            "equal" => MathF.Abs(arguments[0] - arguments[1]) < MathF.Max(_epsilon, _epsilon * MathF.Min(arguments[0], arguments[1])) ? arguments[2] : arguments[3],
            "notequal" => MathF.Abs(arguments[0] - arguments[1]) > MathF.Max(_epsilon, _epsilon * MathF.Min(arguments[0], arguments[1])) ? arguments[2] : arguments[3],
            _ => throw new InvalidDataException($"Unknown function: '{name}'")
        };
    }
}

public sealed class ReflectionContext<TResult, TArguments> : IContext<TResult, TArguments>
{
    private readonly object _source;

    public ReflectionContext(object source)
    {
        _source = source;
    }

    public TResult Resolve(string name, params TArguments[] arguments)
    {
        if (arguments.Length != 0)
        {
            (bool resolved, TResult? value) = CallFunction(name, arguments);
            if (!resolved || value == null) throw new InvalidDataException($"Unknown function: '{name}'");
            return value;
        }

        (bool propertyResolved, TResult? propertyValue) = ResolveProperty(name);
        if (propertyResolved && propertyValue != null) return propertyValue;

        (bool fieldResolved, TResult? fieldValue) = ResolveField(name);
        if (fieldResolved && fieldValue != null) return fieldValue;

        (bool functionResolved, TResult? functionValue) = CallFunction(name, arguments);
        if (functionResolved && functionValue != null) return functionValue;

        throw new InvalidDataException($"Unknown function, property or field: '{name}'");
    }

    public bool Resolvable(string name)
    {
        if (_source.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public) != null) return true;
        if (_source.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public) != null) return true;
        if (_source.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public) != null) return true;
        
        return false;
    }

    private (bool resolved, TResult? value) ResolveProperty(string name)
    {
        PropertyInfo? property = _source.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        object? value = property?.GetValue(_source);
        return (value != null, value == null ? default : (TResult?)value);
    }

    private (bool resolved, TResult? value) ResolveField(string name)
    {
        FieldInfo? field = _source.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        object? value = field?.GetValue(_source);
        return (value != null, value == null ? default : (TResult?)value);
    }

    public (bool resolved, TResult? value) CallFunction(string name, params TArguments[] arguments)
    {
        MethodInfo? methodInfo = _source.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        if (methodInfo == null) return (false, default);
        return (true, (TResult?)methodInfo.Invoke(_source, arguments.Select(value => (object?)value).ToArray()));
    }
}

public sealed class StatsContext<TArguments> : IContext<float, TArguments>
{
    private readonly IPlayer _player;

    public StatsContext(IPlayer player)
    {
        _player = player;
    }

    public bool Resolvable(string name)
    {
        return _player.Entity.Stats.Select(entry => entry.Key).Contains(name);
    }
    public float Resolve(string name, params TArguments[] arguments) => _player.Entity.Stats.GetBlended(name);
}


internal sealed class ValueContext<TResult, TArguments> : IContext<TResult, TArguments>
{
    private readonly TResult _value;

    public ValueContext(TResult value)
    {
        _value = value;
    }

    public bool Resolvable(string name)
    {
        return name == "value";
    }
    public TResult Resolve(string name, params TArguments[] arguments)
    {
        if (name == "value") return _value;

        throw new InvalidDataException($"Unresolvable: '{name}'");
    }
}

internal sealed class BooleanMathContext : IContext<float, float>
{
    private const float _epsilon = float.Epsilon * 2;

    public BooleanMathContext()
    {
    }

    public bool Resolvable(string name)
    {
        return name switch
        {
            "if" => true,
            "not" => true,
            "and" => true,
            "or" => true,
            "true" => true,
            "false" => true,
            _ => false
        };
    }

    public float Resolve(string name, params float[] arguments)
    {
        return name switch
        {
            "if" => AsBool(arguments[0]) ? arguments[1] : arguments[2],
            "not" => AsFloat(!AsBool(arguments[0])),
            "and" => AsFloat(AsBool(arguments[0]) && AsBool(arguments[1])),
            "or" => AsFloat(AsBool(arguments[0]) || AsBool(arguments[1])),
            "true" => _true,
            "false" => _false,
            _ => throw new InvalidDataException($"Unknown function: '{name}'")
        };
    }

    public static bool AsBool(float value) => MathF.Abs(value) > _epsilon;
    public static float AsFloat(bool value) => value ? _true : _false;

    private const float _true = 1;
    private const float _false = 0;
}