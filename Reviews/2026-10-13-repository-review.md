# Shos.DI リポジトリ全体レビュー

- レビュー日: 2026-10-13
- 対象コミット: `50c7e43`（main）
- 対象範囲: 追跡されている全ファイル（`Shos.DI` / `Shos.DI.Sample` / `Shos.DI.WebServer` / `Shos.DI.WebApp` / ルートの `DIContainer.cs` / ビルド設定 / README 英日）
- 検証環境: .NET SDK 10.0.112（Linux / WSL2）。リポジトリを作業用ディレクトリへ `git archive` で展開してビルドし、`Shos.DI.Sample` を実行したほか、コンテナの挙動を確かめる検証用プログラムを別途書いて動かした。

---

## 1. 総評

DI コンテナの仕組みを「1 ファイル・約 200 行」で示し、さらに自作 HTTP サーバー + MVC 風ディスパッチャへ組み込んで ASP.NET Core との対応関係を見せる、という構成は教材としてよくできている。README（英日）の説明も丁寧で、意図的に省いた機能も「Limitations」に正直に書かれている。

ただし実際に動かして確かめると、**README が説明しているコンテナの中心的な挙動（キャッシュによるシングルトン化）は実際には一度も働いていない**。さらに、**明示的に渡した引数が 2 回目以降は無視される**という、教材としては特に誤解を招きやすい不具合もある。Web サーバー側にも、GET 以外のリクエストで応答を閉じない、クエリ文字列でルーティングが壊れる、任意の public メソッドを URL から呼べる、といった問題がある。

教材の価値を保つには、まず「README の説明どおりに動く」状態にすることと、それを守る**単体テスト**を足すことが最優先である。

### 重要度の凡例

| 記号 | 意味 |
| --- | --- |
| 🔴 High | 説明と実際の挙動が食い違う / 誤った結果を返す / ハングやクラッシュにつながる |
| 🟠 Medium | 条件によって不具合になる / 教材として誤解を招く / 保守性を大きく損なう |
| 🟢 Low | スタイル、細かな改善、ドキュメントの軽微なずれ |

---

## 2. 指摘事項一覧

| # | 重要度 | 対象 | 概要 |
| --- | --- | --- | --- |
| C-1 | 🔴 | `Shos.DI/DIContainer.cs:117` | `GetInstance([])` が意図しないオーバーロードに解決され、キャッシュが一切効かない |
| C-2 | 🔴 | `Shos.DI/DIContainer.cs:119-135` | 明示引数付きの解決が「引数の型」だけでキャッシュされ、2 回目以降は渡した引数が無視される |
| C-3 | 🟠 | `Shos.DI/DIContainer.cs:43-51, 142` | コンストラクターの選択順が `GetConstructors()` の返す順に依存している（README の「greediest」とは違う） |
| C-4 | 🟠 | `Shos.DI/DIContainer.cs:123, 198` | 引数に `null` を渡すと `NullReferenceException` |
| C-5 | 🟠 | `Shos.DI/DIContainer.cs:56-64, 128` | 派生型の引数は登録済みの経路ではマッチせず、フォールバックで毎回新しいインスタンスが作られる |
| C-6 | 🟠 | `Shos.DI/DIContainer.cs:36` | `GetInstance(Type)` が private のため、呼び出し側は型名文字列での再検索に頼らざるを得ない |
| C-7 | 🟢 | `Shos.DI/DIContainer.cs:9-10` | `GetInstance<T>` の戻り値が `object?` |
| C-8 | 🟢 | `Shos.DI/DIContainer.cs:66-68` | `ToType` は読み込み済みの全アセンブリから最初に見つかった型を返すため、同名の型があると曖昧 |
| C-9 | 🟢 | `Shos.DI/DIContainer.cs:183-190` | `List<T>.ForEach` があるので独自 `ForEach` 拡張は不要 |
| R-1 | 🟠 | `/DIContainer.cs`（ルート） | 古い版の `DIContainer.cs` が誤ってコミットされている |
| W-1 | 🔴 | `Shos.DI.WebServer/Server.cs:43-44, 59-60` | GET 以外 / WebSocket のリクエストで応答を閉じず、接続がぶら下がる |
| W-2 | 🔴 | `Shos.DI.WebServer/WebAppManager.cs:45-46` | URL から任意の public メソッド（`ToString`、`Equals`、static など）を呼べる |
| W-3 | 🟠 | `Shos.DI.WebServer/Server.cs:96-109` | 500 エラー時にスタックトレースをそのままクライアントへ返す |
| W-4 | 🟠 | `Shos.DI.WebServer/WebAppManager.cs:49-58` | `AbsoluteUri` を分割しているため、クエリ文字列があるとアクション名が壊れる |
| W-5 | 🟠 | `Shos.DI.WebServer/Server.cs:33-39` | `EndGetContext` の例外が捕捉されず、停止時にプロセスが落ちうる |
| W-6 | 🟠 | `Shos.DI.WebServer/Server.cs:30` / `WebAppManager.cs:11-22` | リクエストごとに `WebAppManager` を作り直している（README でも認識済み） |
| W-7 | 🟠 | `Shos.DI.WebServer/WebAppManager.cs:16` | `Apps` フォルダーをカレントディレクトリ基準で探している |
| W-8 | 🟢 | `Shos.DI.WebServer/Server.cs` | `Content-Type` 未設定、UTF-8 BOM を送出、`CurrentCultureIgnoreCase` を使用 など |
| W-9 | 🟢 | `Shos.DI.WebServer/Program.cs:19` | 標準入力がリダイレクトされていると `Console.ReadKey` が例外を投げ、即終了する |
| B-1 | 🔴 | `Shos.DI.WebApp/Shos.DI.WebApp.csproj:8-10` | Linux / macOS では `dotnet build Shos.DI.sln` がエラーで失敗する（README の手順どおりに進められない） |
| B-2 | 🟢 | 各プロジェクト | 警告（CA1050 ×5、CS9113 ×1）が出たまま |
| B-3 | 🟢 | 6 ファイル | UTF-8 BOM が残っている（c993a53 で「BOM 削除」済みのはず） |
| T-1 | 🔴 | リポジトリ全体 | 単体テストがない |
| D-1 | 🟠 | `README.md` / `README.jp.md` | README の記述と実装の食い違い（シングルトン化、greediest、コード片 など） |

---

## 3. 詳細 — DI コンテナ（`Shos.DI/DIContainer.cs`）

### C-1 🔴 `TypeInformation.GetInstance()` が常に `null` を返し、キャッシュが一切効かない

```csharp
public object? GetInstance() => GetInstance([]);              // 117 行目
public object? GetInstance(params object[] parameters) { ... } // 119 行目
object? GetInstance(Type[] parameterTypes) { ... }            // 157 行目（private）
```

空のコレクション式 `[]` は `object[]` にも `Type[]` にも変換できる。C# のオーバーロード解決では、`Type[]` から `object[]` への暗黙変換（配列の共変性）があるため **`Type[]` のほうが「より良い変換先」と判断され、private の `GetInstance(Type[])` が選ばれる**。このメソッドはインスタンスのキャッシュを引くだけで、インスタンスを生成しない。

結果として次のようになる。

- `DIContainer.GetInstance(Type)` の (1)「cached / parameterless」は**常に `null`**。
- 処理は必ずコンストラクターを順に試すループ (2) へ進み、`constructor.Invoke` で**毎回新しいインスタンス**が作られる。このループには**キャッシュへの保存処理がない**。

検証結果（リフレクションで `TypeInformation` を直接呼んだもの）:

```
TypeInformation.GetInstance() for parameterless type => null
GetInstance(object[]{}) => A
```

コンテナ経由で確かめた結果:

```
1 parameterless singleton: False   // Register<A>() 後、GetInstance<A>() を 2 回呼んで同じインスタンスか
2 injected singleton: False        // コンストラクター注入される B でも同様
3 dep shared: False                // 2 つの B が同じ A を共有しているか
```

README には「a registered type is effectively a singleton within one container」「roughly a singleton」と書かれているが、**実際は常にトランジェント**である。サンプルの出力は偶然 README と一致しているが、これはサンプルが各ケースで 1 回しか解決しないためにすぎない。

**修正案**

```csharp
public object? GetInstance() => GetInstance(Array.Empty<object>());
```

あわせて、コンストラクター注入のループで生成したインスタンスもキャッシュするかどうかを決める必要がある。そのうえで、ライフタイムの方針（常にトランジェントにするのか、シングルトンにするのか、登録時に選べるようにするのか）を README と揃えること。private 側のメソッドは `GetCachedInstance(Type[])` のように名前を変えて、オーバーロードの衝突そのものをなくすのが根本的な対策である。

### C-2 🔴 明示引数付きの解決が「引数の型」をキーにキャッシュされ、別の値を渡しても前回のインスタンスが返る

`TypeInformation.GetInstance(params object[] parameters)` は、`parameters` の**型の並び**をキーにして生成したインスタンスを `instanceTable` に保存する。そのため 2 回目以降は、渡した**値**に関係なく、最初に作ったインスタンスが返る。

```
6 explicit args cached: True  diff-arg same inst: True
// c.GetInstance<B>(a) と c.GetInstance<B>(new A()) が同じインスタンスを返す
```

呼び出し側から見ると「渡した引数が黙って捨てられる」ことになり、教材としては特に誤解を招きやすい。明示引数で生成したものは**キャッシュしない**のが自然である（ほかの主要な DI コンテナでも、`ActivatorUtilities.CreateInstance` のような明示引数付きの生成は常に新しいインスタンスを返す）。

### C-3 🟠 コンストラクターの選択が `GetConstructors()` の返す順に依存している

`Type.GetConstructors()` が返す順序は**仕様上保証されていない**。今の実装は「最初に解決できたコンストラクター」を使う。

```
9 ctor order: 0,1,2 chosen=0   // 引数 0 / 1 / 2 個のコンストラクターがあると、引数 0 個のものが選ばれる
```

README は「a simple version of the "greediest constructor that can be satisfied" rule」と説明しているが、実際の挙動は逆（引数の少ないものから試す）で、しかもその順序すら環境依存である。`constructors.OrderByDescending(c => c.GetParameters().Length)` で並べ替えれば、README の説明どおりの挙動になる。

### C-4 🟠 引数に `null` を渡すと `NullReferenceException`

`parameters.Select(parameter => parameter.GetType())`（123 行目、198 行目）は、要素が `null` だと例外になる。

```
8 null arg: NullReferenceException
```

`ArgumentNullException` を投げて意図を明確にするか、`null` 要素はコンストラクター引数の型だけで照合するようにする。

### C-5 🟠 派生型の引数は登録済みの経路でマッチせず、フォールバックで毎回新しく作られる

`TypeInformation` は、引数の型の並びとコンストラクターのシグネチャが**完全に一致するか**で照合する。そのため `C(A a)` に `D : A` を渡すと照合に失敗し、`ActivatorExtensions.GetInstance`（`Type.GetConstructor` は既定のバインダーで代入互換を許す）へフォールバックする。

```
7 derived arg: True, cached: False
```

同じ呼び出しでも、引数の実際の型によって経路もキャッシュの有無も変わってしまう。照合には `IsAssignableFrom` を使って両方の経路を揃えるとよい。

### C-6 🟠 `GetInstance(Type)` が private

公開されているのは `GetInstance<T>()` と `GetInstance(string)` だけである。そのため `WebAppManager` は、手元に `Type` を持っているのに `controllerType.FullName` という文字列に戻し、`ToType` で**全アセンブリを再検索**している（`WebAppManager.cs:41`）。`public object? GetInstance(Type type)` を公開すれば、この遠回りと C-8 の曖昧さをまとめて解消できる。

### C-7 🟢 `GetInstance<T>` の戻り値が `object?`

README の Limitations でも認識されているが、`public T? GetInstance<T>() where T : class => GetInstance(typeof(T)) as T;` にするだけで、サンプル側でのキャストが不要になる。

### C-8 🟢 `ToType` の曖昧さ

`AppDomain.CurrentDomain.GetAssemblies()` の列挙順で最初に見つかった型を返す。プラグイン（`Apps/*.dll`）と本体に同じ完全修飾名の型があると、どちらが選ばれるかは実行してみないとわからない。C-6 を直せば、Web サーバーの経路からはこの問題がなくなる。

### C-9 🟢 その他の細かな点

- `Initialize()` の `pairs` は `List<T>` なので、組み込みの `List<T>.ForEach` で足りる。独自の `EnumerableExtensions.ForEach` は不要（WebServer 側にも同じものが `EnumerableEx` として重複している）。
- `this[ConstructorInfo]` の getter で `parameterTypesTable is null` になるのは「public コンストラクターが 1 つもない型」だけである。インターフェースや抽象クラスを `Register` しても黙って `null` が返る（検証結果 #4、#5）。教材としては、登録時に `ArgumentException` を投げるほうが学びになる。
- `string` などのプリミティブを要求するコンストラクターも、黙って `null` になる（検証結果 #11）。
- 循環依存はスタックオーバーフローになる（README でも認識済み）。解決中の型を `HashSet<Type>` で追跡すれば、数行で検出できる。
- スレッドセーフではない（README でも認識済み）。W-6 を直して `WebAppManager` を 1 つだけにすると、`HttpListener` のコールバックが並行して走るため、この問題が**実際に表面化する**。W-6 を直すときは、`lock` を入れるか `ConcurrentDictionary` に置き換えること。

---

## 4. 詳細 — リポジトリ構成

### R-1 🟠 ルートに古い `DIContainer.cs` がコミットされている

`50c7e43` でルートに `DIContainer.cs`（184 行）が追加されている。`Shos.DI/DIContainer.cs` と比べると、**`TypeArrayComparer` を導入する前の古い版**で、`Dictionary<Type[], ...>` を参照の等価性で比較している（つまりキャッシュが一切ヒットしない版）。

どのプロジェクトにも含まれていないのでビルドには影響しない。しかし README が「コンテナ全体は 1 ファイル」とうたっている以上、ルートで目に入る同名ファイルは読者を確実に混乱させる。**削除を推奨する。**

---

## 5. 詳細 — Web サーバー（`Shos.DI.WebServer`）

### W-1 🔴 GET 以外 / WebSocket のリクエストで応答を閉じない

```csharp
if (!CanAccept(HttpMethod.Get, request.HttpMethod) || request.IsWebSocketRequest)
    return false;     // ← response.Close() されない
```

`OnRequested` は `false` が返ってきても何もしない。そのため `POST` / `HEAD` / `OPTIONS` などのリクエストでは**応答が返らず、クライアントはタイムアウトまで待たされ、サーバー側の接続も解放されない**。`405 Method Not Allowed` を返して `response.Close()` するべきである。

### W-2 🔴 URL から任意の public メソッドを呼べる

```csharp
var action = controllerType.GetMethod(actionName);
return action?.Invoke(controller, []) as string;
```

- `/Home/ToString` → `200 Shos.DI.WebApp.HomeController`（型名が漏れる）
- `/Home/Equals` → `TargetParameterCountException` → 500 + スタックトレース（W-3 と組み合わさる）
- 同名のオーバーロードがあると `AmbiguousMatchException` → 500
- public static メソッドも呼べる

教材としても「ASP.NET Core はアクションをどう選んでいるか」を示すよい機会なので、次のように絞り込むとよい。

```csharp
var action = controllerType.GetMethod(actionName,
                 BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase,
                 Type.EmptyTypes);
if (action is null || action.ReturnType != typeof(string)) return null;
```

### W-3 🟠 500 エラー時にスタックトレースを返している

README では「Developer exception page」に相当すると説明されており、意図的な挙動であることはわかる。ただ、サーバーは `http://+:8080/` で**全インターフェースに公開**し、しかも**管理者権限**（`requireAdministrator`）で動く。コメントやフラグ（例: `#if DEBUG`）で「開発時だけの挙動」であることを明示しておくのが望ましい。

### W-4 🟠 クエリ文字列やフラグメントでルーティングが壊れる

`uri.AbsoluteUri.Split('/')` を使っているため、`/Home/Index?x=1` のアクション名が `Index?x=1` になって 404 になる。末尾にスラッシュを付けた場合や、パーセントエンコードされた URL でも同様に崩れる。`uri.AbsolutePath.Trim('/').Split('/')` か `uri.Segments` を使うべきである。あわせて、

- コントローラー名・アクション名の照合が大文字小文字を区別している（`/home/index` → 404。ASP.NET Core は区別しない）
- `/` や `/Home` に既定のコントローラー / アクション（`Home` / `Index`）がない

という点も、ASP.NET Core との対比として README に書くか、実装に取り込む価値がある。

### W-5 🟠 `EndGetContext` の例外が捕捉されていない

`IsListening` を確認してから `EndGetContext` を呼ぶまでの間に `Stop()` が走ると、`ObjectDisposedException` / `HttpListenerException` が発生しうる。これはスレッドプールのコールバック上で起きるため、**未処理例外としてプロセスごと落ちる**。`try`/`catch` で囲み、停止中であれば静かに抜けるようにする。`BeginGetContext` の state 引数が `null`（16 行目）と `listener`（39 行目）で揃っていない点もあわせて直すとよい。

### W-6 🟠 リクエストごとに `WebAppManager` を作り直している

README の Limitations で認識済みである。ただ、そのせいで C-1 の「シングルトンになっていない」問題が Web 側では表に出ていない、という点は意識しておくべきである。プラグインの DLL が依存先を欠いていると `GetTypes()` が `ReflectionTypeLoadException` を投げ、**すべてのリクエストが 500** になる点にも注意。起動時に 1 回だけ読み込むようにすれば、このエラーも起動時にまとめて検出できる。

### W-7 🟠 `Apps` をカレントディレクトリ基準で探している

`Directory.GetFiles("Apps")` は、`dotnet run --project Shos.DI.WebServer` やリポジトリのルートから起動すると `DirectoryNotFoundException` になる。README に「実行ファイルのフォルダーで起動すること」と書かれてはいるが、`Path.Combine(AppContext.BaseDirectory, "Apps")` にすれば手順そのものが不要になる。`Directory.GetFiles(dir, "*.dll")` を使えば、`EndsWith` によるフィルタも要らない。

### W-8 🟢 細かな HTTP まわりの点

- 200 応答に `Content-Type` を付けていない（`text/html; charset=utf-8` か `text/plain; charset=utf-8` を付ける）。
- `new StreamWriter(stream, Encoding.UTF8)` は**BOM を書き出す**。`new UTF8Encoding(false)` を使う。
- `CanAccept` が `StringComparison.CurrentCultureIgnoreCase` を使っている。HTTP メソッドの比較には `OrdinalIgnoreCase` が適切。
- `AcceptTypes.Contains("text/html")` は完全一致なので、`text/html;q=0.9` や `*/*` だけを送るクライアントは 404 になる（README にも curl での注意書きがある）。
- `app.manifest` の `assemblyIdentity name="MyApplication.app"` はテンプレートのまま残っている。
- `requireAdministrator` は Windows でしか効かず、HTTPS のプレフィックスは証明書をバインドしないと機能しない。既定のプレフィックスを `http://localhost:8080/` にすれば、管理者権限も URL ACL も不要になり、教材として試すまでの手間が大きく減る。

### W-9 🟢 `Console.ReadKey` と標準入力のリダイレクト

標準入力がリダイレクトされている環境（CI、コンテナ、`nohup` など）では、`Console.ReadKey` が `InvalidOperationException` を投げる。その例外は `Main` の `catch` で捕まって `Error:` と表示され、**サーバーは起動直後に終了する**。`Console.IsInputRedirected` を見て、リダイレクト時は `Console.ReadLine()` か `Thread.Sleep(Timeout.Infinite)` で待つようにするとよい。

---

## 6. 詳細 — ビルド / プロジェクト構成

### B-1 🔴 Linux / macOS では `dotnet build Shos.DI.sln` が失敗する

`Shos.DI.WebApp.csproj` のビルド後処理で `xcopy` を `Exec` しているため、Linux ではビルドがエラーで止まる。

```
Shos.DI.WebApp.csproj(9,5): error MSB3073: The command "xcopy /s /y /d ..." exited with code 127.
Build FAILED.
```

README（英日）は Linux/macOS 向けの手順でも、最初に `dotnet build Shos.DI.sln` を実行するよう書いている。しかし実際にはここでビルドが**失敗する**（README は「xcopy は失敗するので手でコピーせよ」とだけ書いており、ビルド全体が失敗することには触れていない）。さらに、次の問題もある。

- パスを引用符で囲んでいないため、空白を含むパスで壊れる。
- `$(SolutionDir)` は、プロジェクト単体でビルドすると `*Undefined*` になる。
- ソリューションに WebServer → WebApp のビルド順序の依存関係が定義されていない。

MSBuild の `Copy` タスクに置き換えれば、OS を問わず動くうえに README の手動コピー手順も不要になる。

```xml
<Target Name="CopyToServerApps" AfterTargets="Build">
  <ItemGroup>
    <AppFiles Include="$(OutDir)**\*" />
  </ItemGroup>
  <Copy SourceFiles="@(AppFiles)"
        DestinationFolder="$(MSBuildThisFileDirectory)..\Shos.DI.WebServer\bin\$(Configuration)\$(TargetFramework)\Apps\%(RecursiveDir)"
        SkipUnchangedFiles="true" />
</Target>
```

### B-2 🟢 警告が出たまま

```
Shos.DI.Sample/Program.cs(50..70): warning CA1050: Declare types in namespaces   ×5
Shos.DI.WebApp/HomeController.cs(3,56): warning CS9113: Parameter 'hogeHoge' is unread.
```

- CA1050: README の Case 2 は `nameof(Foo)`（グローバル名前空間の `"Foo"`）で型名を解決することを前提にしている。名前空間に入れるなら `typeof(Foo).FullName!` に変える必要がある。意図的に残すなら、`#pragma warning disable CA1050` に理由をコメントで添えておく。
- CS9113: `hogeHoge` は「使わない依存も生成される」ことを示すための意図的なものだと README にある。これも警告を抑制して、その意図をコメントに残すとよい。

`Directory.Build.props` で `AnalysisLevel=latest-Minimum` を有効にしているのだから、警告ゼロを維持し、可能なら `TreatWarningsAsErrors` まで進めることを推奨する。

### B-3 🟢 BOM が残っている

`c993a53`（「BOM削除」）のあとも、次のファイルに UTF-8 BOM が残っている。

- `Shos.DI.Sample/Shos.DI.Sample.csproj`
- `Shos.DI.WebApp/Shos.DI.WebApp.csproj`
- `Shos.DI.WebServer/EnumerableEx.cs`
- `Shos.DI.WebServer/Shos.DI.WebServer.csproj`
- `Shos.DI.WebServer/WebAppManager.cs`
- `Shos.DI.WebServer/app.manifest`

`.editorconfig` に `charset = utf-8` を追加して統一することを推奨する（現在 `.editorconfig` はない）。

### その他

- `Shos.DI.csproj` には NuGet パッケージ用のメタデータ（`PackageId`、`Version`、`Authors`、`Description` など）がない。教材なので必須ではないが、README で配布しないことを明記しておくと親切である。
- CI（GitHub Actions など）がない。B-1 のように「Linux でビルドが通らない」状態は、`ubuntu-latest` で `dotnet build` と `dotnet test` を回すだけで検出できる。

---

## 7. テスト

### T-1 🔴 単体テストがない

C-1 / C-2 / C-3 は、どれも「同じ型を 2 回解決する」「異なる引数を渡す」という数行のテストで検出できる不具合である。サンプルは各ケースで 1 回しか解決しないため、これらの不具合が表に出なかった。

`Shos.DI.Tests`（xUnit または MSTest）を追加し、少なくとも次のケースを押さえることを推奨する。

| テスト | 期待値（方針を決めたうえで） |
| --- | --- |
| 引数なしコンストラクターの型を 2 回解決 | 方針に従ってシングルトンかトランジェントか |
| 注入される型を 2 回解決 | 同上 |
| 依存先が共有されるか | 同上 |
| 明示引数を変えて 2 回解決 | 2 つは別のインスタンスで、それぞれ渡した値で構築されている |
| 複数コンストラクター | 最も多くの引数を満たせるものが選ばれる |
| 派生型を引数に渡す | 一致するコンストラクターが選ばれる |
| `null` 引数 | 明確な例外が出る |
| 未登録の依存（引数なしコンストラクターを持つ） | 生成される（README の `Coo` の例） |
| インターフェース / 抽象型 | 方針に従って `null` か例外か |
| 循環依存 | 検出を実装した場合は例外 |
| 型名での登録・解決 | 型を直接指定した場合と同じ結果になる |

`WebAppManager.Split` のルーティング（クエリ文字列、末尾スラッシュ、大文字小文字）も、テストしやすいよう `internal static` にして `InternalsVisibleTo` でテストから呼べるようにするとよい。

---

## 8. ドキュメント（README.md / README.jp.md）

### D-1 🟠 README と実装の食い違い

| README の記述 | 実際 |
| --- | --- |
| 「a registered type is effectively a singleton within one container」（How the container works 1.）/「roughly a singleton」（Limitations） | C-1 のとおり、`GetInstance<T>()` の経路では常にトランジェント。キャッシュされるのは明示引数付きの経路だけで、しかもそこでは C-2 のとおり引数が無視される |
| 「a simple version of the "greediest constructor that can be satisfied" rule」 | C-3 のとおり、`GetConstructors()` の返す順に試すだけ（実際には引数の少ないものが先に選ばれた） |
| `types.ToList().ForEach(container.Register);`（WebAppManager のコード片、ASP.NET Core 対応表） | 実際の実装は `.ToList()` を `types` に代入してから `types.ForEach(container.Register)` を呼んでいる（`EnumerableEx` を使用） |
| 「*every* public type in *every* assembly」 | `Assembly.GetTypes()` は非 public の型（コンパイラが生成する型を含む）も返すので、「every type」が正確 |
| Linux/macOS 手順の `dotnet build Shos.DI.sln` | B-1 のとおり、ビルドは失敗で終わる |
| 「about 180 lines of C#」 | 現在の `Shos.DI/DIContainer.cs` は 199 行。誤差の範囲だが、改修後に合わせて更新するとよい |

C-1 / C-2 / C-3 を修正したら、該当箇所の説明とコード片を英日両方で更新すること。逆に、もし「キャッシュしない」方針を選ぶなら、README の「Cache lookup」の段落を書き直す必要がある。

---

## 9. 推奨する対応順序

1. **C-1 / C-2 を修正し、T-1 のテストを追加する**（教材としての正しさの回復。README の説明の根幹にかかわる）
2. **R-1** ルートの古い `DIContainer.cs` を削除する
3. **W-1 / W-2 / W-4 / W-5** Web サーバーの不具合を修正する（ハング、任意メソッドの呼び出し、ルーティング、クラッシュ）
4. **B-1** `xcopy` を MSBuild の `Copy` タスクに置き換え、CI を追加する
5. **C-3 / C-6** greediest 方式でのコンストラクター選択と `GetInstance(Type)` の公開。あわせて W-6（WebAppManager の単一化）とスレッドセーフ化を行う
6. **D-1** README（英日）の更新、B-2 / B-3 の警告と BOM の整理

---

## 付録: 検証に使ったコード（抜粋）

```csharp
var c = new DIContainer();
c.Register<A>(); c.Register<B>();
ReferenceEquals(c.GetInstance<A>(), c.GetInstance<A>());              // => False（C-1）
ReferenceEquals(c.GetInstance<B>(), c.GetInstance<B>());              // => False（C-1）
ReferenceEquals(c.GetInstance<B>(new A()), c.GetInstance<B>(new A())); // => True （C-2: 2 回目の引数が無視される）
c.GetInstance<B>((object)null!);                                       // => NullReferenceException（C-4）
// Multi(), Multi(A), Multi(A, B) をすべて登録・解決可能にしても Multi() が選ばれる（C-3）

public class A {}
public class B { public A A; public B(A a) => A = a; }
```

`Shos.DI.Sample` の実行結果は、README に載っている出力と 4 ケースとも一致した。
