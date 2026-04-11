namespace JsonPatchLib.Expressions;

public class MathParser
{
    private readonly ITokenizer<float> _tokenizer;

    public MathParser(ITokenizer<float> tokenizer)
    {
        _tokenizer = tokenizer;
    }

    public INode<float, float, float> ParseExpression()
    {
        INode<float, float, float> expression = ParseAddSubtract();

        if (_tokenizer.Token != Token.EOF)
        {
            throw new SyntaxException("Unexpected characters at end of expression");
        }

        return expression;
    }

    INode<float, float, float> ParseAddSubtract()
    {
        INode<float, float, float> leftOperand = ParseMultiplyDivide();

        while (true)
        {
            Func<float, float, float>? operation = null;
            if (_tokenizer.Token == Token.Add)
            {
                operation = (a, b) => a + b;
            }
            else if (_tokenizer.Token == Token.Subtract)
            {
                operation = (a, b) => a - b;
            }

            if (operation == null) return leftOperand;

            _tokenizer.NextToken();

            INode<float, float, float> rightOperand = ParseMultiplyDivide();

            leftOperand = new Nodes.Binary<float, float, float>(leftOperand, rightOperand, operation);
        }
    }

    INode<float, float, float> ParseMultiplyDivide()
    {
        INode<float, float, float> leftOperand = ParseUnary();

        while (true)
        {
            Func<float, float, float>? operation = null;
            if (_tokenizer.Token == Token.Multiply)
            {
                operation = (a, b) => a * b;
            }
            else if (_tokenizer.Token == Token.Divide)
            {
                operation = (a, b) => a / b;
            }

            if (operation == null) return leftOperand;

            _tokenizer.NextToken();

            INode<float, float, float> rightOperand = ParseUnary();

            leftOperand = new Nodes.Binary<float, float, float>(leftOperand, rightOperand, operation);
        }
    }

    INode<float, float, float> ParseUnary()
    {
        while (true)
        {
            if (_tokenizer.Token == Token.Add)
            {
                _tokenizer.NextToken();
                continue;
            }

            if (_tokenizer.Token == Token.Subtract)
            {
                _tokenizer.NextToken();
                INode<float, float, float> rightOperand = ParseUnary();
                return new Nodes.Unary<float, float, float>(rightOperand, (a) => -a);
            }

            return ParseNumber();
        }
    }

    INode<float, float, float> ParseNumber()
    {
        if (_tokenizer.Token != Token.Identifier && _tokenizer.Token != Token.Number && _tokenizer.Token != Token.OpenParenthesis) throw new SyntaxException($"Unexpected token: {_tokenizer.Token}");

        if (_tokenizer.Token == Token.Number)
        {
            Nodes.Value<float, float, float> node = new(_tokenizer.Value);
            _tokenizer.NextToken();
            return node;
        }

        if (_tokenizer.Token == Token.OpenParenthesis)
        {
            _tokenizer.NextToken();

            INode<float, float, float> node = ParseAddSubtract();

            if (_tokenizer.Token != Token.CloseParenthesis) throw new SyntaxException("Missing close parenthesis");
            _tokenizer.NextToken();

            return node;
        }

        string name = _tokenizer.Identifier;
        _tokenizer.NextToken();

        if (_tokenizer.Token != Token.OpenParenthesis)
        {
            return new Nodes.Variable<float, float>(name);
        }

        _tokenizer.NextToken();

        List<INode<float, float, float>> arguments = new();
        while (true)
        {
            arguments.Add(ParseAddSubtract());

            if (_tokenizer.Token == Token.Comma)
            {
                _tokenizer.NextToken();
                continue;
            }

            break;
        }


        if (_tokenizer.Token != Token.CloseParenthesis) throw new SyntaxException("Missing close parenthesis");
        _tokenizer.NextToken();

        return new Nodes.FunctionCall<float, float>(name, arguments.ToArray());
    }


    #region Convenience Helpers

    public static INode<float, float, float> Parse(string str)
    {
        return Parse(new FloatTokenizer(new StringReader(str)));
    }

    public static INode<float, float, float> Parse(FloatTokenizer tokenizer)
    {
        MathParser parser = new(tokenizer);
        return parser.ParseExpression();
    }

    #endregion
}
