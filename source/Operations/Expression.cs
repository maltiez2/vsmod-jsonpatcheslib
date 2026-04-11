using JsonPatchLib.Expressions;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace JsonPatchLib;

public static partial class Operations
{
    public static bool Expression(ICoreAPI api, JsonPatch patch, JToken asset)
    {
        if (patch.Path == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "Expression operation json patch does not have Path specified");
            return false;
        }

        if (patch.Value == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "Expression operation json patch does not have Value specified");
            return false;
        }

        IEnumerable<JToken> values = patch.Path.Get(asset, api);

        string? expressionString = patch.Value.AsString();
        if (expressionString == null)
        {
            LoggerUtil.Error(api, typeof(Operations), "Expression operation json patch does not have Value specified or it has wrong format");
            return false; 
        }

        INode<float, float, float> expression;
        try
        {
            expression = MathParser.Parse(expressionString);
        }
        catch (Exception exception)
        {
            LoggerUtil.Error(api, typeof(Operations), $"Expression operation json patch has malformed expression: {exception}");
            return false;
        }

        MathContext mathContext = new();
        BooleanMathContext booleanContext = new();

        if (!values.Any())
        {
            return false;
        }

        values.Foreach(value =>
        {
            if (patch.Value.Token == null || value is not JValue targetValue)
            {
                return;
            }
            
            switch (value.Type)
            {
                case JTokenType.Integer:
                    {
                        ValueContext<float, float> valueContext = new((long)targetValue.Value);
                        CombinedContext<float, float> context = new([mathContext, booleanContext, valueContext]);
                        float result = expression.Evaluate(context);
                        value.Replace(new JValue(result));
                    }
                    break;
                case JTokenType.Float:
                    {
                        ValueContext<float, float> valueContext = new((float)targetValue.Value);
                        CombinedContext<float, float> context = new([mathContext, booleanContext, valueContext]);
                        float result = expression.Evaluate(context);
                        value.Replace(new JValue(result));
                    }
                    break;
                case JTokenType.Boolean:
                    {
                        ValueContext<float, float> valueContext = new(BooleanMathContext.AsFloat((bool)targetValue.Value));
                        CombinedContext<float, float> context = new([mathContext, booleanContext, valueContext]);
                        bool result = BooleanMathContext.AsBool(expression.Evaluate(context));
                        value.Replace(new JValue(result));
                    }
                    break;
                default:
                    break;
            }
        });

        return true;
    }
}
