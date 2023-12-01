using LitTemplate;
// ReSharper disable StringLiteralTypo
namespace test;

public class Tests {
    [SetUp] public void Setup() { }
    
    [Test]
    public void TestVariable() {
        const string template = @"test1: {{ test1 }}, " +
                                @"test2: {{ test2 }}, " +
                                @"test3: {{ test3 }}, ";

        var temp1 = new Template(new Dictionary<string, int> {
            { "test1", 100 },
            { "test2", 200 },
            { "test3", 300 }
        });
        var content1 = temp1.Parse(template);
        Log.Info(content1);

        var temp2 = new Template(new { test1 = 100, test2 = 200, test3 = 300 });
        var content2 = temp2.Parse(template);
        Log.Info(content2);
    }

    [Test]
    public void TestForeach() {
        const string template1 = @"{% foreach kv in this %}" +
                                 @"kv.Key: {{ kv.Key }}, kv.Value: {{ kv.Value }}    " +
                                 @"{% endfor %}";
        var temp1 = new Template(new Dictionary<string, int> {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        });
        var content1 = temp1.Parse(template1);
        Log.Info(content1);

        const string template2 = @"{% foreach kv in this %}" +
                                 @"kv: {{ kv }}, " +
                                 @"{% endfor %}";
        var temp2 = new Template(new List<string> {
            "Hello", "World", "Bye"
        });
        var content2 = temp2.Parse(template2);
        Log.Info(content2);
    }

    [Test]
    public void TestIf() {
        const string template = @"{% if test > 10 %}" +
                                @"the value {{ test }} > 10 " +
                                @"{% else %}" +
                                @"the value {{ test }} <= 10 " +
                                @"{% endif %}";

        var temp1 = new Template(new { test = 1 });
        var content1 = temp1.Parse(template);
        Log.Info(content1);

        var temp2 = new Template(new { test = 11 });
        var content2 = temp2.Parse(template);
        Log.Info(content2);
    }
}
// ReSharper restore StringLiteralTypo
