using System.Collections;
using System.Reflection;
using System.Text;
namespace LitTemplate;

public class Template {
    private const char TOKEN_BEGIN = '{';
    private const char TOKEN_END = '}';
    private const char TOKEN_STATEMENT = '%';
    private const string FOR_BEGIN = "foreach";
    private const string TOKEN_IN = "in";
    private const string FOR_END = "endfor";
    private const string IF_BEGIN = "if";
    private const string TOKEN_ELSE = "else";
    private const string IF_END = "endif";
    private static readonly StringBuilder STRING_BUFFER = new StringBuilder();

    private readonly ValueProvider _valueProvider;

    private struct Range {
        public int Begin;
        public readonly int End; //include
        public Range(int b, int e) {
            Begin = b;
            End = e;
        }
        public int Length => End - Begin + 1;
    }

    public Template(object data) => _valueProvider = new ValueProvider(data);

    public string Parse(string content) => Parse(content, new Range(0, content.Length - 1));

    private string Parse(string content, Range range) {
        var buffer = new StringBuilder(range.Length);
        var index = range.Begin;
        while (index < range.End) {
            var ch1 = content[index];
            var ch2 = content[index + 1];

            switch (ch1) {
            case TOKEN_BEGIN when ch2 == TOKEN_BEGIN:
                index = ParseVariable(content, new Range(index + 2, range.End), buffer);
                break;
            case TOKEN_BEGIN when ch2 == TOKEN_STATEMENT:
                index = ParseStatement(content, new Range(index + 2, range.End), buffer);
                break;
            default:
                index++;
                buffer.Append(ch1);
                break;
            }
            if (index == range.End) {
                buffer.Append(content[range.End]);
            }
        }
        return buffer.ToString();
    }

    private int ParseVariable(string content, Range range, StringBuilder stringBuilder) {
        var index = FindIndexOfChar(content, range, TOKEN_END, TOKEN_END);
        if (index == -1) {
            throw new Exception(
                "syntax error, can't found [}}] : " + content.Substring(range.Begin));
        }

        STRING_BUFFER.Append(content, range.Begin, index - range.Begin);
        var keyword = STRING_BUFFER.ToString().Trim();
        STRING_BUFFER.Clear();

        stringBuilder.Append(_valueProvider.GetStringInPath(keyword));
        return index + 2;
    }

    private int ParseStatement(string content, Range range, StringBuilder stringBuilder) {
        var nextIndex = FindFirstWord(content, range, out var word);
        var tokenEndIndex = FindIndexOfChar(content, new Range(nextIndex, range.End),
            TOKEN_STATEMENT, TOKEN_END);
        if (tokenEndIndex == -1) {
            throw new Exception(
                "syntax error, can't found [%}] : " + content.Substring(range.Begin));
        }
        if (word == FOR_BEGIN) {
            return ParseForStatement(content, range, new Range(nextIndex, tokenEndIndex),
                stringBuilder);
        }
        if (word == IF_BEGIN) {
            return ParseIfStatement(content, range, new Range(nextIndex, tokenEndIndex),
                stringBuilder);
        }
        Log.Error("syntax error, can't support statement : " + word);
        return nextIndex;
    }

    private int ParseForStatement(string content, Range range, Range rangeFor,
        StringBuilder stringBuilder) {
        rangeFor.Begin = FindFirstWord(content, rangeFor, out var name);
        rangeFor.Begin = FindFirstWord(content, rangeFor, out var keyIn);
        rangeFor.Begin = FindFirstWord(content, rangeFor, out var dataPath);

        if (keyIn != TOKEN_IN) {
            throw new Exception(
                "syntax error, can't found [in] : " + content.Substring(range.Begin));
        }

        var forEndIndex = FindTargetEnd(content, new Range(rangeFor.Begin, range.End), FOR_BEGIN,
            FOR_END, out var checkStart);
        if (forEndIndex == -1) {
            throw new Exception("syntax error, can't found right foreach end : " +
                                content.Substring(range.Begin));
        }

        var foreachData = _valueProvider.GetValueInPath(dataPath);
        if (foreachData is IEnumerable enumerable) {
            foreach (var obj in enumerable) {
                _valueProvider.PushLocalValue(name, obj);
                stringBuilder.Append(Parse(content, new Range(rangeFor.End + 2, checkStart - 1)));
                _valueProvider.PopLocalValue(name, obj);
            }
        }
        return forEndIndex;
    }

    private int ParseIfStatement(string content, Range range, Range rangeIf,
        StringBuilder stringBuilder) {
        rangeIf.Begin = FindFirstWord(content, rangeIf, out var name);
        rangeIf.Begin = FindFirstWord(content, rangeIf, out var check);
        rangeIf.Begin = FindFirstWord(content, rangeIf, out var val);

        var ifEndEnd = FindTargetEnd(content, new Range(rangeIf.Begin, range.End), IF_BEGIN, IF_END,
            out var ifEndBegin);
        if (ifEndEnd == -1) {
            throw new Exception("syntax error, can't found right if end : " +
                                content.Substring(range.Begin));
        }
        var ifElseEnd = FindTargetEnd(content, new Range(rangeIf.Begin, range.End), IF_BEGIN,
            TOKEN_ELSE, out var ifElseBegin);

        if (CheckIfExpression(name, check, val)) {
            var endIndex = ifElseEnd == -1 ? ifEndBegin - 1 : ifElseBegin - 1;
            stringBuilder.Append(Parse(content, new Range(rangeIf.End + 2, endIndex)));
        } else {
            if (ifElseEnd != -1) {
                stringBuilder.Append(Parse(content, new Range(ifElseEnd, ifEndBegin - 1)));
            }
        }
        return ifEndEnd;
    }

    private static int FindTargetEnd(string content, Range range, string beginStr, string endStr,
        out int checkStart) {
        var depth = 1;
        for (var i = range.Begin; i <= range.End;) {
            var index = FindIndexOfChar(content, new Range(i, range.End), TOKEN_BEGIN,
                TOKEN_STATEMENT);
            if (index == -1) {
                checkStart = range.Begin;
                return -1;
            }
            var nextIndex = FindFirstWord(content, new Range(index + 2, range.End), out var word);
            if (word == beginStr) {
                depth++;
                i = nextIndex;
                continue;
            }
            if (word == endStr) {
                depth--;
                i = nextIndex;
                if (depth == 0) {
                    checkStart = index;
                    return FindIndexOfChar(content, new Range(nextIndex, range.End),
                        TOKEN_STATEMENT, TOKEN_END) + 2;
                }
                continue;
            }
            i = nextIndex;
        }
        checkStart = range.Begin;
        return -1;
    }

    private static int FindFirstWord(string content, Range range, out string word) {
        var findChar = false;
        int index;
        for (index = range.Begin; index <= range.End; index++) {
            var ch = content[index];
            if (char.IsWhiteSpace(ch)) {
                if (!findChar) {
                    continue;
                }
                break;
            }
            STRING_BUFFER.Append(ch);
            findChar = true;
        }

        word = STRING_BUFFER.ToString();
        STRING_BUFFER.Clear();
        return index;
    }

    private static int FindIndexOfChar(string content, Range range, char ch1, char ch2) {
        for (var i = range.Begin; i < range.End; ++i) {
            var c1 = content[i];
            var c2 = content[i + 1];
            if (c1 == ch1 && c2 == ch2) {
                return i;
            }
        }

        return -1;
    }

    private bool CheckIfExpression(string checkName, string check, string checkVal) {
        var compare = CompareIfData(checkName, checkVal);
        if (compare is null) {
            return false;
        }
        if (compare == 0) {
            return check == "==" || check == "<=" || check == ">=";
        }
        return compare < 0 ? check == "<" || check == "<=" : check == ">" || check == ">=";
    }

    private int? CompareIfData(string checkName, string checkVal) {
        try {
            var ifData = _valueProvider.GetValueInPath(checkName);
            var ifVal = Convert.ChangeType(checkVal, ifData.GetType().UnderlyingSystemType);
            if (ifData is IComparable ac && ifVal is IComparable bc) {
                return ac.CompareTo(bc);
            }
        } catch (Exception e) {
            Log.Error(e.Message);
            throw;
        }
        return null;
    }

    public static Dictionary<string, object> GetKeyValues(object data) {
        var dict = new Dictionary<string, object>();
        const BindingFlags bindFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var fieldArray = data.GetType().GetFields(bindFlags);
        if (fieldArray.Length > 0) {
            foreach (var fieldInfo in fieldArray) {
                dict.Add(fieldInfo.Name, fieldInfo.GetValue(data));
            }
        }
        var propertyArray = data.GetType().GetProperties(bindFlags);
        if (propertyArray.Length > 0) {
            foreach (var property in propertyArray) {
                dict.Add(property.Name, property.GetValue(data));
            }
        }
        return dict;
    }

    private static object GetObjectValue(object data, string key) {
        if (data is Dictionary<string, object> dictionary) {
            return dictionary[key];
        }

        const BindingFlags bindFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var fieldArray = data.GetType().GetFields(bindFlags);
        if (fieldArray.Length > 0) {
            var field = fieldArray.FirstOrDefault(f => f.Name == key);
            if (field != null) {
                return field.GetValue(data);
            }
        }
        var propertyArray = data.GetType().GetProperties(bindFlags);
        if (propertyArray.Length > 0) {
            var property = propertyArray.FirstOrDefault(p => p.Name == key);
            if (property != null) {
                return property.GetValue(data);
            }
        }
        return default;
    }

    private static object GetObjectInPath(object data, string path) {
        var obj = data;
        if (!path.Contains('.')) {
            return GetObjectValue(data, path);
        }
        var keywords = path.Split('.');
        foreach (var s in keywords) {
            obj = GetObjectValue(obj, s);
        }
        return obj;
    }

    private static string GetStringValueInPath(object data, string path) {
        var obj = GetObjectInPath(data, path);
        return obj == null ? string.Empty : obj.ToString();
    }
}
