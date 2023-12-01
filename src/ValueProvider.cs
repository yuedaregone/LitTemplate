using System.Collections;
namespace LitTemplate;

public class ValueProvider {
    private readonly RuntimeObject _runtimeObject;
    private readonly IDictionary _dictionary;
    private readonly Dictionary<string, ValueProvider> _members =
        new Dictionary<string, ValueProvider>();
    private readonly List<KeyValuePair<string, object>> _additions =
        new List<KeyValuePair<string, object>>();

    public ValueProvider(object target) {
        if (target is IDictionary dict) {
            _dictionary = dict;
        } else {
            _runtimeObject = new RuntimeObject(target);
        }
    }

    public void PushLocalValue(string name, object value)
        => _additions.Add(new KeyValuePair<string, object>(name, value));

    public void PopLocalValue(string name, object value) {
        for (var i = _additions.Count - 1; i >= 0; i--) {
            if (name.Equals(_additions[i].Key, StringComparison.Ordinal) &&
                ReferenceEquals(value, _additions[i].Value)) {
                _additions.RemoveAt(i);
                break;
            }
        }
    }

    public string GetStringInPath(string path) {
        var obj = GetValueInPath(path);
        return obj == null ? string.Empty : obj.ToString();
    }

    public object GetValueInPath(string path) {
        if (path.Equals("this", StringComparison.OrdinalIgnoreCase)) {
            return _dictionary ?? _runtimeObject.Target;
        }
        if (path.StartsWith("this.", StringComparison.OrdinalIgnoreCase)) {
            path = path.Substring(5);
        }

        var dotIndex = path.IndexOf('.');
        if (dotIndex == -1) {
            return TryGetAdditionValue(path, out var val) ? val : GetObjectValue(path);
        }

        var firstName = path.Substring(0, dotIndex);

        ValueProvider valueProvider;
        if (TryGetAdditionValue(firstName, out var objValue)) {
            valueProvider = new ValueProvider(objValue);
        } else {
            if (!_members.TryGetValue(firstName, out valueProvider)) {
                valueProvider = new ValueProvider(GetObjectValue(firstName));
                _members.Add(firstName, valueProvider);
            }
        }
        var newPath = path.Substring(dotIndex + 1);
        return valueProvider.GetValueInPath(newPath);
    }

    private bool TryGetAdditionValue(string name, out object value) {
        for (var i = _additions.Count - 1; i >= 0; i--) {
            if (name.Equals(_additions[i].Key, StringComparison.Ordinal)) {
                value = _additions[i].Value;
                return true;
            }
        }
        value = null;
        return false;
    }

    private object GetObjectValue(string name) {
        if (_dictionary != null && _dictionary.Contains(name)) {
            return _dictionary[name];
        }

        if (_runtimeObject != null) {
            var result = _runtimeObject.GetPropertyValue(name, false, true);
            return result ?? _runtimeObject.GetFieldValue(name, false, true);
        }
        return null;
    }
}
