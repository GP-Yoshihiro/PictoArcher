using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PictoArcher.Core;
using PictoArcher.Game;
using UnityEngine;

namespace PictoArcher.Tests.EditMode
{
    /// <summary>
    /// 設計の規則（docs/development-rules.md §2、設計書 §11）を破ると落ちるテスト。
    /// 違反は全部集めてから、場所と直し方をまとめて出す。
    /// 例外は規則ごとにテストの中だけに持つ。例外を足すのは設計の変更なので、PR に理由を書く。
    /// </summary>
    public class ArchitectureGuardTests
    {
        const BindingFlags AllDeclared =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        static readonly string[] LifecycleMethods =
        {
            "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable", "OnDestroy",
            "OnApplicationPause", "OnApplicationFocus", "OnApplicationQuit", "OnGUI", "OnBecameVisible", "OnBecameInvisible",
        };

        // ---- リフレクションの規則 ----

        [Test]
        public void NoLifecycleMethodsOutsideMain()
        {
            var allowed = new[] { typeof(Main) };
            var violations = new List<string>();
            foreach (var type in UnityObjectTypes())
            {
                if (allowed.Contains(type)) continue;
                foreach (var method in type.GetMethods(AllDeclared))
                    if (LifecycleMethods.Contains(method.Name)) violations.Add(type.Name + "." + method.Name);
            }
            Report(violations, "標準ライフサイクル（Update など）は Main だけが書けます。シーンの Tick から呼ばれるメソッドにしてください。");
        }

        [Test]
        public void NoCollisionCallbacksOutsideRelay()
        {
            var allowed = new[] { typeof(CollisionRelay2D) };
            var pattern = new Regex(@"^(OnTrigger|OnCollision)(Enter|Stay|Exit)(2D)?$");
            var violations = new List<string>();
            foreach (var type in UnityObjectTypes())
            {
                if (allowed.Contains(type)) continue;
                foreach (var method in type.GetMethods(AllDeclared))
                    if (pattern.IsMatch(method.Name)) violations.Add(type.Name + "." + method.Name);
            }
            Report(violations, "当たり判定の通知は CollisionRelay2D だけが受けます。CollisionRelay2D を付けて、System の Tick で DrainEntered / Touching を見てください。");
        }

        [Test]
        public void NoMutableStatics()
        {
            var violations = new List<string>();
            foreach (var type in OwnTypes())
            {
                if (type.IsEnum) continue;
                foreach (var field in type.GetFields(AllDeclared))
                {
                    if (!field.IsStatic || field.IsLiteral) continue;
                    bool mutable = !field.IsInitOnly || IsCollection(field.FieldType);
                    if (mutable) violations.Add(type.Name + "." + MemberNameOf(field));
                }
            }
            Report(violations, "static の書き換えられる値は持てません（再生のたびに残ります）。シーンや GameContext が持つインスタンスにしてください。");
        }

        [Test]
        public void NoPublicFieldsOnSerializedTypes()
        {
            var violations = new List<string>();
            foreach (var type in OwnTypes())
            {
                bool serialized = IsUnityObject(type) || (type.IsSerializable && !type.IsEnum && !typeof(Delegate).IsAssignableFrom(type));
                if (!serialized) continue;
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    violations.Add(type.Name + "." + field.Name);
            }
            Report(violations, "インスペクターに出す値は public のフィールドにせず、[SerializeField] _camelCase と読み取り専用のプロパティにしてください。");
        }

        [Test]
        public void NoPublicSettersOnScriptableObjects()
        {
            var violations = new List<string>();
            foreach (var type in OwnTypes().Where(t => t.IsSubclassOf(typeof(ScriptableObject))))
                foreach (var property in type.GetProperties(AllDeclared))
                    if (property.SetMethod != null && property.SetMethod.IsPublic) violations.Add(type.Name + "." + property.Name);
            Report(violations, "ScriptableObject のデータは読み取り専用です。public の setter を消し、プレイ中に変わる値はシーンの System が持ってください。");
        }

        [Test]
        public void OnlyFrameworkTypesAreInheritable()
        {
            var violations = new List<string>();
            var own = new HashSet<Assembly> { CoreAssembly, GameAssembly };
            foreach (var type in OwnTypes())
            {
                if (!type.IsClass || (type.IsAbstract && type.IsSealed)) continue; // static class は対象外
                if (type.IsAbstract)
                {
                    if (!IsInheritableFramework(type)) violations.Add(type.Name + "（abstract）");
                }
                else if (!type.IsSealed)
                {
                    violations.Add(type.Name + "（sealed がない）");
                }

                var baseType = type.BaseType;
                if (baseType != null && own.Contains(baseType.Assembly) && !IsInheritableFramework(baseType))
                    violations.Add(type.Name + "（" + baseType.Name + " を継承）");
            }
            Report(violations, "継承してよいのは SceneBase、DefinitionAsset<T>、DefinitionAsset<T> を直接継承した抽象クラスだけです。ほかのクラスは sealed にして、組み合わせで作ってください。");
        }

        [Test]
        public void NamespacesAreFixed()
        {
            var violations = new List<string>();
            foreach (var type in Types(CoreAssembly))
                if (type.Namespace != "PictoArcher.Core") violations.Add(type.FullName + " → PictoArcher.Core");
            foreach (var type in Types(GameAssembly))
                if (type.Namespace != "PictoArcher.Game") violations.Add(type.FullName + " → PictoArcher.Game");
            Report(violations, "名前空間は PictoArcher.Core と PictoArcher.Game だけです。フォルダを分けても名前空間は分けません。");
        }

        [Test]
        public void NamingRules()
        {
            var privateField = new Regex(@"^_[a-z][A-Za-z0-9]*$");
            var pascal = new Regex(@"^[A-Z][A-Za-z0-9]*$");
            var violations = new List<string>();
            foreach (var type in OwnTypes())
            {
                if (type.IsInterface && !Regex.IsMatch(type.Name, @"^I[A-Z]"))
                    violations.Add(type.Name + "（interface は I で始める）");
                if (type.IsSubclassOf(typeof(SceneBase)) && !type.Name.EndsWith("Scene", StringComparison.Ordinal))
                    violations.Add(type.Name + "（SceneBase の子は Scene で終わる）");
                if (type.IsDefined(typeof(ExtensionAttribute), false) && !type.Name.EndsWith("Extensions", StringComparison.Ordinal))
                    violations.Add(type.Name + "（拡張メソッドの static クラスは Extensions で終わる）");
                if (type.IsEnum) continue;

                foreach (var field in type.GetFields(AllDeclared))
                {
                    if (field.Name.StartsWith("<", StringComparison.Ordinal)) continue; // 自動プロパティの裏のフィールド
                    if (field.IsLiteral || (field.IsStatic && field.IsInitOnly))
                    {
                        if (!pascal.IsMatch(field.Name)) violations.Add(type.Name + "." + field.Name + "（const と static readonly は PascalCase）");
                    }
                    else if (!field.IsStatic && field.IsPrivate)
                    {
                        if (type.GetEvent(field.Name, AllDeclared) != null) continue; // イベントの裏のフィールド（イベントと同じ名前）
                        if (!privateField.IsMatch(field.Name)) violations.Add(type.Name + "." + field.Name + "（private のフィールドは _camelCase）");
                    }
                }
            }
            Report(violations, "命名の規則（docs/development-rules.md §2）に合わせて名前を変えてください。");
        }

        // ---- ソースの文字列の規則 ----

        [Test]
        public void NoCoroutinesOrAsync() => ReportSourceMatches(
            AllSources(), new string[0],
            new[]
            {
                @"\bStartCoroutine\b", @"\bStopCoroutine\b", @"\bStopAllCoroutines\b", @"\bWaitForSeconds\w*",
                @"\basync\b", @"\bawait\b", @"\bInvokeRepeating\b", @"\bInvoke\s*\(\s*(nameof\b|"")",
            },
            "コルーチン・async・文字列の Invoke は使えません。時間は Tick の deltaTime を足して数えてください（Core の FireTimer などを参考に）。");

        [Test]
        public void NoFindOrCameraMain() => ReportSourceMatches(
            AllSources(), new string[0],
            new[]
            {
                @"\bGameObject\s*\.\s*Find\w*", @"\bFind(Object|Objects|FirstObject|AnyObject)\w*", @"\bFindWithTag\b",
                @"\bFindGameObjectsWithTag\b", @"\w*[Tt]ransform\s*\.\s*Find\s*\(", @"\bCamera\s*\.\s*main\b",
            },
            "Find 系と Camera.main は使えません。インスペクターの [SerializeField] でつなぐか、Init の引数で受け取ってください。");

        [Test]
        public void NoSendMessage() => ReportSourceMatches(
            AllSources(), new string[0],
            new[] { @"\b(SendMessage|SendMessageUpwards|BroadcastMessage)\b" },
            "SendMessage 系は使えません。interface（IHittable など）のメソッドを直接呼んでください。");

        [Test]
        public void TimeOnlyInMainAndClock() => ReportSourceMatches(
            AllSources(), new[] { "Main.cs", "GameClock.cs" },
            new[] { @"\bTime\s*\." },
            "Time. を使えるのは Main と GameClock だけです。Tick の deltaTime か GameContext.Clock を使ってください。");

        [Test]
        public void BootstrapApisOnlyInMain()
        {
            ReportSourceMatches(
                AllSources(), new[] { "Main.cs" },
                new[] { @"\bResources\s*\.\s*Load", @"\bDontDestroyOnLoad\b", @"\bRuntimeInitializeOnLoadMethod\b", @"\bDefaultExecutionOrder\b" },
                "起動の仕組み（Resources.Load、DontDestroyOnLoad、RuntimeInitializeOnLoadMethod、DefaultExecutionOrder）は Main だけが使えます。");
            ReportSourceMatches(
                AllSources(), new string[0],
                new[] { @"\bExecuteAlways\b", @"\bExecuteInEditMode\b" },
                "ExecuteAlways / ExecuteInEditMode は使えません。エディタの道具は Scripts/Editor に置いてください。");
        }

        [Test]
        public void CoreUsesNoUnityComponents() => ReportSourceMatches(
            Sources(CoreFolder), new string[0],
            new[] { @"\b(MonoBehaviour|ScriptableObject|GameObject|Component|Transform|Rigidbody2D|Collider2D|Time|Physics2D|Input|Debug|Resources)\b" },
            "Core は純粋な C# です。Unity の部品を使う処理は Game に置き、Core には計算だけを残してください。");

        [Test]
        public void FileNameMatchesTypeName()
        {
            var violations = new List<string>();
            CheckFileNames(CoreAssembly, CoreFolder, violations);
            CheckFileNames(GameAssembly, GameFolder, violations);
            Report(violations, "1ファイルに1つの型にして、ファイル名を型の名前と同じにしてください（移すときは git mv で .meta も一緒に）。");
        }

        [Test]
        public void NoPersistentUnityEventCalls()
        {
            var pattern = new Regex(@"m_Calls:[ \t]*\r?\n[ \t]*- ");
            var root = Path.Combine(Application.dataPath, "_Project");
            var violations = new List<string>();
            foreach (var path in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (!path.EndsWith(".prefab", StringComparison.Ordinal) && !path.EndsWith(".unity", StringComparison.Ordinal)) continue;
                var text = File.ReadAllText(path);
                foreach (Match match in pattern.Matches(text))
                    violations.Add(Relative(path) + ":" + LineOf(text, match.Index));
            }
            Report(violations, "インスペクターで UnityEvent（ボタンの OnClick など）に関数をつながないでください。UiButtonRelay を付けて、シーンの Tick で ConsumeClicked を見てください。");
        }

        // ---- 下ごしらえ ----

        static Assembly CoreAssembly => typeof(SceneFlow<>).Assembly;
        static Assembly GameAssembly => typeof(Main).Assembly;
        static string CoreFolder => Path.Combine(Application.dataPath, "_Project/Scripts/Core");
        static string GameFolder => Path.Combine(Application.dataPath, "_Project/Scripts/Game");

        static IEnumerable<Type> OwnTypes() => Types(CoreAssembly).Concat(Types(GameAssembly));

        static IEnumerable<Type> UnityObjectTypes() => OwnTypes().Where(IsUnityObject);

        static bool IsUnityObject(Type type) =>
            type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsSubclassOf(typeof(ScriptableObject));

        /// <summary>自分たちが書いた型。コンパイラや Unity が作った型（ラムダのキャッシュ &lt;&gt;c など）は除く。</summary>
        static IEnumerable<Type> Types(Assembly assembly) => assembly.GetTypes().Where(t => !IsGenerated(t));

        static bool IsGenerated(Type type)
        {
            for (var t = type; t != null; t = t.DeclaringType)
            {
                if (t.IsDefined(typeof(CompilerGeneratedAttribute), false)) return true;
                if (t.IsDefined(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute), false)) return true;
                if (t.Name.StartsWith("<", StringComparison.Ordinal) || t.Name.StartsWith("$", StringComparison.Ordinal)
                    || t.Name.StartsWith("__", StringComparison.Ordinal)
                    || t.Name.StartsWith("UnitySourceGenerated", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        static bool IsInheritableFramework(Type type)
        {
            if (type == typeof(SceneBase)) return true;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DefinitionAsset<>)) return true;
            var baseType = type.BaseType;
            return type.IsAbstract && baseType != null && baseType.IsGenericType
                && baseType.GetGenericTypeDefinition() == typeof(DefinitionAsset<>);
        }

        static bool IsCollection(Type type) =>
            type.IsArray || (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type));

        /// <summary>自動プロパティやイベントの裏のフィールド &lt;X&gt;k__BackingField は X として出す。</summary>
        static string MemberNameOf(FieldInfo field)
        {
            var match = Regex.Match(field.Name, @"^<(.+)>k__BackingField$");
            return match.Success ? match.Groups[1].Value + "（自動プロパティかイベント）" : field.Name;
        }

        static void CheckFileNames(Assembly assembly, string folder, List<string> violations)
        {
            var files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
            var typeNames = new HashSet<string>();
            foreach (var type in Types(assembly).Where(t => t.DeclaringType == null))
            {
                var name = StripArity(type.Name);
                typeNames.Add(name);
                int count = files.Count(f => Path.GetFileNameWithoutExtension(f) == name);
                if (count != 1) violations.Add(type.FullName + "（" + name + ".cs が " + count + " 個）");
            }
            foreach (var file in files)
                if (!typeNames.Contains(Path.GetFileNameWithoutExtension(file)))
                    violations.Add(Relative(file) + "（同じ名前の型がない）");
        }

        static string StripArity(string name)
        {
            int tick = name.IndexOf('`');
            return tick < 0 ? name : name.Substring(0, tick);
        }

        static IEnumerable<string> AllSources() => Sources(CoreFolder).Concat(Sources(GameFolder));

        static IEnumerable<string> Sources(string folder) => Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);

        static void ReportSourceMatches(IEnumerable<string> files, string[] allowedFileNames, string[] patterns, string howToFix)
        {
            var regexes = patterns.Select(p => new Regex(p)).ToArray();
            var violations = new List<string>();
            foreach (var path in files)
            {
                if (allowedFileNames.Contains(Path.GetFileName(path))) continue;
                var code = StripCommentsAndStrings(File.ReadAllText(path));
                foreach (var regex in regexes)
                    foreach (Match match in regex.Matches(code))
                        violations.Add(Relative(path) + ":" + LineOf(code, match.Index) + "  " + match.Value);
            }
            Report(violations, howToFix);
        }

        /// <summary>
        /// コメントを消し、文字列と文字の中身を空白にする（引用符は残す）。改行は残すので行番号はずれない。
        /// </summary>
        static string StripCommentsAndStrings(string source)
        {
            var result = new StringBuilder(source.Length);
            int i = 0;
            while (i < source.Length)
            {
                char c = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';
                if (c == '/' && next == '/')
                {
                    while (i < source.Length && source[i] != '\n') i++;
                }
                else if (c == '/' && next == '*')
                {
                    i += 2;
                    while (i < source.Length && !(source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/'))
                    {
                        if (source[i] == '\n') result.Append('\n');
                        i++;
                    }
                    i += 2;
                }
                else if (c == '"')
                {
                    bool verbatim = (i >= 1 && source[i - 1] == '@') || (i >= 2 && source[i - 2] == '@' && source[i - 1] == '$');
                    result.Append('"');
                    i++;
                    while (i < source.Length)
                    {
                        char s = source[i];
                        if (verbatim && s == '"' && i + 1 < source.Length && source[i + 1] == '"') { result.Append("  "); i += 2; continue; }
                        if (!verbatim && s == '\\') { result.Append("  "); i += 2; continue; }
                        if (s == '"') break;
                        result.Append(s == '\n' ? '\n' : ' ');
                        i++;
                    }
                    result.Append('"');
                    i++;
                }
                else if (c == '\'')
                {
                    result.Append('\'');
                    i++;
                    while (i < source.Length && source[i] != '\'' && source[i] != '\n')
                    {
                        if (source[i] == '\\') { result.Append(' '); i++; }
                        result.Append(' ');
                        i++;
                    }
                    result.Append('\'');
                    i++;
                }
                else
                {
                    result.Append(c);
                    i++;
                }
            }
            return result.ToString();
        }

        static int LineOf(string text, int index)
        {
            int line = 1;
            for (int i = 0; i < index; i++) if (text[i] == '\n') line++;
            return line;
        }

        static string Relative(string path)
        {
            var root = Application.dataPath.Replace('\\', '/');
            var normalized = path.Replace('\\', '/');
            return normalized.StartsWith(root, StringComparison.Ordinal) ? "Assets" + normalized.Substring(root.Length) : normalized;
        }

        static void Report(List<string> violations, string howToFix) =>
            Assert.IsEmpty(violations, howToFix + "\n" + string.Join("\n", violations));
    }
}
