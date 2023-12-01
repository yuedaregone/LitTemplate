using System.Reflection;
namespace LitTemplate;

public class RuntimeObject {
    private readonly Type _type;
    private readonly Dictionary<string, List<MethodInfo>> _methods =
        new Dictionary<string, List<MethodInfo>>();
    private readonly Dictionary<string, FieldInfo> _fields =
        new Dictionary<string, FieldInfo>();
    private readonly Dictionary<string, PropertyInfo> _properties =
        new Dictionary<string, PropertyInfo>();
    private RuntimeObject _parent;

    public object Target { get; }

    private bool _loadFields;
    private bool _loadProperties;
    private bool _loadMethods;

    public RuntimeObject(object target) {
        Target = target;
        _type = target.GetType();
    }

    public RuntimeObject(object target, Type type) {
        Target = target;
        _type = type;
    }
    
    public RuntimeObject GetParent() {
        if (_parent != null) {
            return _parent;
        }
        
        if (_type == typeof(object)) {
            return null;
        }
        _parent = new RuntimeObject(Target, _type.BaseType);
        return _parent;
    }
    
    public object GetFieldValue(string name, bool isStatic = false, bool checkParent = false) {
        if (!_loadFields) {
            LoadField();
        }
        if (_fields.TryGetValue(name, out var field)) {
            return field.GetValue(isStatic ? null : Target);
        }
        return checkParent ? GetParent()?.GetFieldValue(name, isStatic, true) : null;
    }
    
    public void SetFieldValue(string name, object value, bool isStatic = false, bool checkParent = false) {
        if (!_loadFields) {
            LoadField();
        }
        if (_fields.TryGetValue(name, out var field)) {
            field.SetValue(isStatic ? null : Target, value);
            return;
        }
        if (checkParent) {
            GetParent()?.SetFieldValue(name, value, isStatic, true);
        }
    }
    
    public object GetPropertyValue(string name, bool isStatic = false, bool checkParent = false) {
        if (!_loadProperties) {
            LoadProperty();
        }
        if (_properties.TryGetValue(name, out var property)) {
            return property.GetValue(isStatic ? null : Target);
        }
        return checkParent ? GetParent()?.GetPropertyValue(name, isStatic, true) : null;
    }
    
    public void SetPropertyValue(string name, object value, bool isStatic = false, bool checkParent = false) {
        if (!_loadProperties) {
            LoadProperty();
        }
        if (_properties.TryGetValue(name, out var property)) {
            property.SetValue(isStatic ? null : Target, value);
            return;
        }
        if (checkParent) {
            GetParent()?.SetPropertyValue(name, value, isStatic, true);
        }
    }
    
    public object InvokeMethod(string name, object[] args, bool isStatic = false, bool checkParent = false) {
        if (!_loadMethods) {
            LoadMethod();
        }
        if (_methods.TryGetValue(name, out var methodList)) {
            var method = GetTargetMethodInfo(methodList, args);
            if (method != null) {
                return method.Invoke(isStatic ? null : Target, args);
            }
        }
        return checkParent ? GetParent()?.InvokeMethod(name, args, isStatic, true) : null;
    }        

    private void LoadMethod() {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        
        var methods = _type.GetMethods(flags);
        if (methods.Length == 0) {
            return;
        }
        foreach (var method in methods) {
            AddMemberInfo(method, _methods);
        }
        _loadMethods = true;
    }

    private void LoadField() {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        
        var fields = _type.GetFields(flags);
        if (fields.Length == 0) {
            return;
        }
        foreach (var field in fields) {
            AddMemberInfo(field, _fields);
        }
        _loadFields = true;
    }

    private void LoadProperty() {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        
        var properties = _type.GetProperties(flags);
        if (properties.Length == 0) {
            return;
        }
        foreach (var propertyInfo in properties) {
            AddMemberInfo(propertyInfo, _properties);
        }
        _loadProperties = true;
    }

    private static void AddMemberInfo<T>(T member, IDictionary<string, List<T>> dict) where T : MemberInfo {
        var name = member.Name;
        if (!dict.TryGetValue(name, out var list)) {
            list = new List<T>();
            dict.Add(name, list);
        }
        list.Add(member);
    }

    private static void AddMemberInfo<T>(T member, IDictionary<string, T> dict)
        where T : MemberInfo {
        var name = member.Name;
        dict[name] = member;
    }

    private static MethodInfo GetTargetMethodInfo(List<MethodInfo> methodInfos, object[] args) {
        var argTypes = args.Select(arg => arg.GetType()).ToArray();
        
        bool IsSame(IReadOnlyList<ParameterInfo> parameterInfos, IReadOnlyList<Type> types) {
            for (var i = 0; i < types.Count; i++) {
                if (parameterInfos[i].ParameterType != types[i]) {
                    return false;
                }
            }
            return true;
        }
        
        foreach (var methodInfo in methodInfos) {
            var parameters = methodInfo.GetParameters();
            if (parameters.Length != argTypes.Length) {
                continue;
            }

            if (!IsSame(parameters, argTypes)) {
                continue;
            }

            return methodInfo;
        }
        return null;
    }
}
