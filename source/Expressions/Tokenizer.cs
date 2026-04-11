using System.Globalization;
using System.Text;

namespace JsonPatchLib.Expressions;

public class FloatTokenizer : ITokenizer<float>
{
    private readonly TextReader _reader;
    private char _currentCharacter;
    private char _previousCharacter = '\0';
    private char _beforePreviousCharacter = '\0';
    private Token _currentToken;
    private float _number;
    private string _identifier = "";

    public FloatTokenizer(TextReader reader)
    {
        _reader = reader;
        NextChar();
        NextToken();
    }

    public Token Token => _currentToken;
    public float Value => _number;
    public string Identifier => _identifier;

    public void NextToken()
    {
        while (char.IsWhiteSpace(_currentCharacter))
        {
            NextChar();
        }

        if (ProcessSpecialSymbol()) return;
        if (ProcessDigit()) return;
        _ = ProcessIdentifier();
    }

    private void NextChar()
    {
        int code = _reader.Read();
        char character = code < 0 ? '\0' : (char)code;
        if (!char.IsWhiteSpace(character))
        {
            if (!char.IsWhiteSpace(_previousCharacter)) _beforePreviousCharacter = _previousCharacter;
            if (!char.IsWhiteSpace(_currentCharacter)) _previousCharacter = _currentCharacter;
        }
        _currentCharacter = character;
    }

    private bool ProcessSpecialSymbol()
    {
        switch (_currentCharacter)
        {
            case '\0':
                _currentToken = Token.EOF;
                return true;

            case '+':
                if (_previousCharacter == 'E' && char.IsDigit(_beforePreviousCharacter))
                {
                    return false;
                }
                NextChar();
                _currentToken = Token.Add;
                return true;

            case '-':
                if (_previousCharacter == 'E' && char.IsDigit(_beforePreviousCharacter))
                {
                    return false;
                }
                NextChar();
                _currentToken = Token.Subtract;
                return true;

            case '*':
                NextChar();
                _currentToken = Token.Multiply;
                return true;

            case '/':
                NextChar();
                _currentToken = Token.Divide;
                return true;

            case '(':
                NextChar();
                _currentToken = Token.OpenParenthesis;
                return true;

            case ')':
                NextChar();
                _currentToken = Token.CloseParenthesis;
                return true;

            case ',':
                NextChar();
                _currentToken = Token.Comma;
                return true;
            default:
                return false;
        }
    }

    private bool ProcessDigit()
    {
        if (
            !char.IsDigit(_currentCharacter) && _currentCharacter != '.' ||
            !char.IsDigit(_previousCharacter) && _currentCharacter == 'E' ||
            _currentCharacter == '-' && _previousCharacter != 'E' ||
            _currentCharacter == '+' && _previousCharacter != 'E'
        ) return false;

        StringBuilder numberString = new();
        bool haveDecimalPoint = false;
        while (
            char.IsDigit(_currentCharacter) || 
            !haveDecimalPoint && _currentCharacter == '.' ||
            _currentCharacter == 'E' ||
            _previousCharacter == 'E' && _currentCharacter == '-' ||
            _previousCharacter == 'E' && _currentCharacter == '+'
        )
        {
            numberString.Append(_currentCharacter);
            haveDecimalPoint = _currentCharacter == '.';
            NextChar();
        }

        _number = float.Parse(numberString.ToString(), CultureInfo.InvariantCulture);
        _currentToken = Token.Number;
        return true;
    }

    private bool ProcessIdentifier()
    {
        if (!char.IsLetter(_currentCharacter) && _currentCharacter != '_') return false;

        StringBuilder identifier = new();

        while (char.IsLetterOrDigit(_currentCharacter) || _currentCharacter == '_')
        {
            identifier.Append(_currentCharacter);
            NextChar();
        }

        _identifier = identifier.ToString();
        _currentToken = Token.Identifier;
        return true;
    }
}
