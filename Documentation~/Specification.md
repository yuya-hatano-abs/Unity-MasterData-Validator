# Master Data Validator 仕様

この文書は、パッケージ `com.yuya-hatano.masterdata-validator` 0.1.0 の動作を、実装に沿って説明します。画面に出る文言は、実装の文字列です。

## 1. 概要と構成

Runtime と Editor は別の asmdef です。

- `MasterDataTool.Runtime` は参照が空で、`autoReferenced` は true です。プレイヤービルドからも参照できます。
- `MasterDataTool.Editor` は `MasterDataTool.Runtime` だけを参照し、プラットフォームは Editor です。`autoReferenced` は false です。Newtonsoft.Json.dll を直接参照します。
- テスト用の `MasterDataTool.Editor.Tests` は Editor 専用です。

`[MasterRef]` と `[MasterRequired]` は `[Conditional("UNITY_EDITOR")]` です。属性の定義は Runtime にあり、検査は Editor が行います。

マスタの基底は `MasterDataBase`（ScriptableObject）と、レコードの `IntIdMasterRecord` / `StringIdMasterRecord` です。外部から `MasterRecordBase` を直接継承することはできません。

## 2. JSON の形式

取り込みが成功するルートは、オブジェクトの中の `records` 配列です。

```json
{ "records": [ { "id": 1, "name": "ポーション" } ] }
```

- 空の JSON、構文エラー、末尾の余分なトークンは、種類 `InvalidJson`、重大度 Error です。
- ルートが配列、または `records` が配列でないときは、種類 `MissingRecordsArray`、重大度 Error です。
- `records` の要素がオブジェクトでないときは、種類 `RecordNotObject`、重大度 Error です。
- コメントは無視します。
- 同じオブジェクト内の重複したプロパティ名は、種類 `InvalidJson`、重大度 Error です。

キーは、シリアライズされるフィールド名と完全一致です。比較は ordinal（大文字と小文字を区別）です。`name` と `Name` は別のキーです。

- フィールドに無いキーは、種類 `UnknownField`、重大度 Warning です。そのキーは無視します。
- フィールドに対応するキーが無いときは、種類 `MissingField`、重大度 Warning です。レコードは作り、そのフィールドは既定値のままです。
- 文字列の `null` は空文字として取り込みます。問題にはしません。
- 整数、数値、bool、enum の `null` は、種類 `TypeMismatch`、重大度 Error です。

`records` 配列そのものがパースできない場合、インポートの `Succeeded` は false です。フィールド単位の問題があっても、`records` 配列まで読めていれば `Succeeded` は true です。

ファイルの読み込みは UTF-8（BOM あり・なし）です。パスが空、ファイルが無い、読み込みに失敗したときは、種類 `FileNotFound`、重大度 Error で、`Succeeded` は false です。

## 3. 型の扱い

JSON の値を、別の型へ自動では変換しません。

- 整数フィールド（`int` / `long`）には、JSON の整数だけを指定できます。数値の文字列や小数は `TypeMismatch`（Error）です。`int` の範囲外は `ValueOutOfRange`（Error）です。
- 数値フィールド（`float` / `double`）には、JSON の整数または小数だけを指定できます。
- `bool` には JSON の `true` / `false` だけを指定できます。
- `string` には JSON の文字列だけを指定できます。数値は変換しません。
- `enum` には、定義された定数名の文字列、または基になる整数を指定できます。

対応するフィールド型は、`int`、`long`、`float`、`double`、`bool`、`string`、`enum`、配列、`List<>` です。

それ以外は、種類 `UnsupportedFieldType`、重大度 Warning です。同じフィールド名では 1 回だけ出します。入れ子の `[Serializable]` クラスも、この未対応です。`UnityEngine.Object` 系も同様です。値は既定値のままです。

文字列の `null` は空文字に、`null` の配列と `List<>` は空のコレクションに補正します。

## 4. ID

整数 ID のレコードは `IntIdMasterRecord`、文字列 ID は `StringIdMasterRecord` です。JSON のキーはどちらも `id` です。

照合に使う `Key` は文字列です。

- 整数 ID は、カルチャに依存しない 10 進表現です。`0` は `"0"` であり、空にはなりません。
- 文字列 ID の `null` は空文字です。
- 比較は ordinal です。`A` と `a` は別の ID です。

重複の検査は、同じテーブルの中だけで行います。最初の 1 件は報告せず、2 件目以降を 1 件ずつ Error（`DuplicateId`）にします。`null` のレコードと、`Key` が `null` または空のレコードは、重複の対象にしません。

文字列 ID が空、または空白だけのときは、`[MasterRequired]` が無くても Error（`MissingRequiredField`）です。整数の `0` はこの空判定に入りません。

## 5. コード生成

コード生成のメニューと、ウィンドウ上のボタンはありません。エディタ API として、`MasterCodeGenerator.Generate`、`MasterCodeGenerator.GenerateFromJson`、`MasterCodeWriter.WriteToDirectory`、`MasterCodeWriter.WriteToAssetFolder` を提供します。

`GenerateFromJson` は、取り込みと同じ `{"records":[...]}` からスキーマを推論します。

- `id` 以外のキーがフィールドになります。キーは英小文字で始まり、その後は英数字と `_` だけです（lowerCamelCase の先頭が小文字）。キーワードや、大文字で始まるキーは Error です。
- 整数 ID は、すべての `id` が `int` に収まる整数のときです。1 件でも範囲外なら推論は失敗します。文字列 ID は、すべての `id` が文字列のときです。混在や `null` は Error です。
- `int` に収まる整数は `int`、範囲外の整数は `long` です。整数と小数が混ざると `float` です。
- 空配列だけ、または値が無く `null` だけのキーは、それぞれ `List<string>` と `string` として推論します。
- オブジェクト、および要素がオブジェクトの配列は、フィールドを生成せず Warning です。
- `Id`、`Key`、`Equals`、`GetHashCode`、`GetType`、`ToString` など、予約されたプロパティ名になるキーは Error です。
- `records` が空だと推論できません。

生成されるフィールドには `[SerializeField]` だけが付きます。`[MasterRef]` と `[MasterRequired]` は付きません。

## 6. 属性

### [MasterRef]

他マスタの ID を参照します。参照先の型は、`MasterDataBase` を継承した abstract でない型です。

対応するフィールド型は、`int`、`string`、`List<int>`、`List<string>`、`int[]`、`string[]` です。参照先の ID の種類（int / string）と、フィールドの種類は一致している必要があります。

- `NoneValue` を指定しない `int` は、`0` を含むすべての値を参照として検査します。`0` が見つからないときは、メッセージに「0 を『参照なし』として扱う場合は、MasterRef に NoneValue = 0 を指定してください。」を足します。
- `NoneValue` と等しい整数は検査しません。
- 文字列の `null` と空文字は検査しません。
- 文字列のフィールドに `NoneValue` を付けると、種類 `InvalidReferenceDefinition`、重大度 Error です。
- 参照先の型が、検証に渡したテーブルに無いときは、種類 `ReferenceTargetUnavailable`、重大度 Error です。そのフィールドの参照切れは調べません。

見つからない ID は、種類 `BrokenReference`、重大度 Error です。

### [MasterRequired]

「空」の定義は次のとおりです。

- `string` は、`null`、空文字、空白だけの文字列です。
- `List<T>` と配列は、`null` または要素 0 件です。要素の中身は見ません。

対応する型は `string`、`List<T>`、配列です。`int`、`bool`、`float`、`enum`、`UnityEngine.Object` 系に付けると、種類 `InvalidRequiredDefinition`、重大度 Error です。シリアライズ対象でないフィールドに付けた場合も同じ種類です。

空の値は、種類 `MissingRequiredField`、重大度 Error です。`[MasterRef]` と併用できます。空なら必須のエラー、値があって参照先に無いなら参照切れのエラーです。

レコードそのものが `null` のときも、`MissingRequiredField`（Error）です。

## 7. 検証

問題の種類は `ValidationIssueKind` です。日本語名は結果一覧の種類ドロップダウンに使います。

| 名前 | 重大度 | 日本語ラベル | 出す場所 |
| --- | --- | --- | --- |
| DuplicateId | Error | ID の重複 | 重複の検証器 |
| BrokenReference | Error | 参照切れ | 参照の検証器 |
| MissingRequiredField | Error | 必須項目の欠落 | 必須の検証器 |
| ImportFailed | Error | インポートの失敗 | 構文、ファイル、`records`、要素、抽象型、フィールド名の重複を変換 |
| TypeMismatch | Error | 型の不一致 | インポートの型不一致を変換 |
| ValueOutOfRange | Error | 値の範囲外 | インポートの範囲外を変換 |
| UnknownField | Warning | 未知のフィールド | インポートの未知キーを変換 |
| MissingField | Warning | フィールドの欠落 | インポートの欠落キーを変換 |
| UnsupportedFieldType | Warning | 未対応のフィールド型 | インポートの未対応型を変換 |
| ValidatorFailure | Error | 検証器の異常 | 検証器が null を返した、または例外を投げたとき |
| ReferenceTargetUnavailable | Error | 参照先なし | 参照の検証器 |
| InvalidReferenceDefinition | Error | 参照定義の不正 | 参照の検証器 |
| InvalidRequiredDefinition | Error | 必須定義の不正 | 必須の検証器 |
| MultipleMasterAssets | Warning | アセットの重複 | プロジェクト内のアセット収集 |
| AmbiguousMasterName | Warning | マスタ名の曖昧さ | プロジェクト内のアセット収集、または取り込み中の同名除外 |

標準の検証器は、重複、参照、必須の順です。インポート由来の Warning は Error のままではなく、Warning のまま結果に残します。

キーが無いフィールドは、取り込みが `MissingField`（Warning）を出します。そのフィールドが必須の文字列で、既定の空文字のままなら、必須の検証器が同じレコードに `MissingRequiredField`（Error）も出します。欠落 1 件で、警告とエラーの両方が残ります。

`IsValid` は Error が 0 件のとき true です。Warning だけなら true です。

## 8. パイプラインと保存条件

1 回の実行は、JSON の取り込み、参照先テーブルの組み立て、検証、必要なら保存、の順です。

- インポートの `Succeeded` が false のときは、状態はインポート失敗です。重複・参照・必須の検証器は走らせません。
- フィールドの型不一致は `Succeeded` を false にしません。レコードは残り、検証器も走ります。型不一致の Error は結果に含まれるため、Error があれば保存しません。
- 検証の Error が 1 件でもあると保存しません。このときも、計算できる差分は結果に残します。
- Warning だけ、または問題が無いときは保存できます。
- 「検証」は保存しません。「検証して保存」は、Error が無いときだけ保存します。

検証に失敗しても、既存アセットが読めていれば差分の計算は試みます。

## 9. ウィンドウ

メニューは `Tools/Master Data Tool/Importer` です。ウィンドウ名は `Master Data Tool`、最小サイズは 420×360 です。

入力とボタンの文言は次のとおりです。

- ラベル「対象マスタ」「JSON ファイル」「保存先アセット」「検証結果」
- ボタン「再読み込み」「参照...」「検証」「検証して保存」
- チェック「Console にログを出す」
- マスタ型が無いとき「マスタ型(MasterDataBase<T> を継承した、abstract でないクラス)が見つかりません。」
- 未実行のとき「検証結果は、ここに表示されます。」

「対象マスタ」の候補は、`MasterDataBase<T>` を継承した abstract でない型です。保存先が空のときだけ、`Assets/MasterData/<型名>.asset` を入れます。

「JSON ファイル」は、入力した文字列を `File.Exists` と `File.ReadAllText` にそのまま渡します。拡張子は `.json`（大文字小文字は区別しない）で、ファイルが存在する必要があります。「参照...」は OS のファイル選択で、戻り値はその絶対パスです。`Assets/` で始まることは検査しません。相対パスをプロジェクトのルートへ変換しません。

「保存先アセット」は `AssetPathValidator` の規則です。`Assets/` で始まり `.asset` で終わる、ルートでないパスだけを受け付けます。バックスラッシュ、末尾の `/`、連続した `/`、空の区切り、`..`、ファイル名に使えない文字は不可です。形式が正しければ、ファイルがまだ無くてもエラーにしません。別の型のアセットがあるときはエラーです。同じ型があるときは「既存のアセットを更新します。」と出します。

結果一覧には「エラー (件数)」「警告 (件数)」と、種類のドロップダウン（先頭は「すべて」）があります。一覧の高さは 240 です。行を選ぶと、その問題のアセットを `Selection` で選択し、Project ウィンドウで Ping します。アセットが無い行は「この問題には、対応するアセットがありません。」または「アセットが見つかりません(未保存の可能性があります): 」と出します。同じ行の再クリックは、選択イベントを再度送りません。

「Console にログを出す」の初期値はオンです。オフにすると、その実行の Console 出力だけを止めて、検証と保存の結果は変わりません。

## 10. 差分表示

「検証して保存」で、既存アセットの型が一致し、シリアライズした records が同じでないとき、確認ダイアログを出します。文言は「既存のアセットを更新」、ボタンは「更新する」と「キャンセル」です。内容が同じときはダイアログを出さず、書き込みません。状態は保存成功で、結果は「内容が同じため、書き込みませんでした」です。

差分の Foldout は閉じて始まります。開くと、追加は `+`、削除は `-`、変更は `~` の行です。変更の下に「フィールド名: 旧値 → 新値」が続きます。

計算できない条件は、既存側または新規側に、重複した Key、空の Key、`null` のレコードがあるときです。このとき件数は出さず、理由の文を出します。計算中の例外は「差分を計算できませんでした」です。Foldout の要約は「差分を計算できません。」のあとにその理由が続きます。

確認ダイアログに渡す要約が空文字になるのは、差分レポートが null のときだけです。計算不可のレポートは null ではないので、ダイアログが出る場合の要約は空になりません。ダイアログ自体は、シリアライズした records が同じなら出しません。計算不可でも、シリアライズ比較が同じならダイアログは出ません。

並び順だけが違い、フィールド値の差分が無いときは「ID・フィールドの変更なし(並び順のみ変更)」です。追加や変更もあるときは、要約の末尾に「(並び順の変更あり)」が付きます。

表示の上限は、追加・削除・変更が各 20 件、1 レコードあたり 5 フィールド、値は 40 文字、コレクションは 5 要素です。超えた分は「…」で省略します。詳細の ScrollView の高さ上限は 160 です。

検証に失敗しても、計算できた差分は結果に残します。既存アセットが無い、または型が違うときは、差分は null です。

## 11. 参照先の収集

参照先は、プロジェクト内の `MasterDataBase` アセットから集めます。

- 同じ型のアセットが 2 件以上ある型は、参照先のテーブルに入れません。種類 `MultipleMasterAssets`、重大度 Warning です。
- 名前が同じ別の型も外します。種類 `AmbiguousMasterName`、重大度 Warning です。
- 今回インポートするマスタは、同じ型の既存テーブルを外し、JSON から作ったテーブルで検証します。参照の検査対象は、この 1 テーブルです。参照先には、プロジェクト側に残した他の型も使います。

## 12. ログ

1 回の実行で、Console に出すのは開始と終了の 2 行です。先頭は `[MasterDataTool] ` です。問題 1 件ごとの行は出しません。終了行に、レコード件数、エラー件数、警告件数、所要時間をまとめます。

- 開始は Info です。
- 終了は、検証失敗、インポート失敗、保存失敗、依頼不正、予期しない失敗が Error です。
- 保存のキャンセル、または警告が 1 件以上のときは Warning です。
- それ以外は Info です。

チェックをオフにしても、開始行と終了行の文字列組み立ては行われます。Console へは渡しません。

プロジェクトのアセット収集 `Collect()` は、この 2 行を出す処理の外で呼びます。ここで例外になると、開始行は出ません。

## 13. 設定の保存

EditorPrefs に保存するのは次です。

- 選択中のマスタ型（型の FullName）
- JSON ファイルのパス
- 保存先アセットのパス
- 「Console にログを出す」のオン/オフ

キーは `MasterDataTool.` + プロジェクト識別子 + `.MasterType` / `.JsonPath` / `.AssetPath` / `.LogToConsole` です。識別子は、`Application.dataPath` を UTF-8 で SHA-256 にし、小文字 16 進の先頭 16 文字にしたものです。プロジェクトを移動したりコピーしたりすると `dataPath` が変わり、別のキーになります。設定は引き継がれません。

ログのキーが無いときはオンです。空文字のパスも保存します。同じ値は書き直しません。

JSON 選択ダイアログの直前のディレクトリと、結果一覧のフィルターは保存しません。フィルターの初期値は、エラーと警告の両方を表示し、種類は「すべて」です。ウィンドウを組み直すと、この初期値に戻ります。

## 14. 動作環境

- Unity 6000.0 以降（`package.json` の `unity`）
- `com.unity.nuget.newtonsoft-json` 3.2.1
- EditMode テスト 628 件（`[Test]` の数）

## 15. 既知の制限

- 同じ型のマスタアセットが複数ある型は、参照先に使えません。検証の文脈も、1 つのマスタ型につきテーブルは 1 つです。
- 入れ子の `[Serializable]` クラスと、`UnityEngine.Object` 系のフィールドは、インポート未対応です。`UnsupportedFieldType` の Warning になります。必須チェックの対象型でもありません。
- コード生成の GUI はありません。
- `Collect()` の例外は、実行ログに出ません。
- 結果一覧のフィルターは保存されず、ウィンドウの再構築で初期状態に戻ります。
- 同じ結果行の再クリックでは、アセットの選択をやり直しません。
- 差分を計算できないとき、ダイアログの要約は空にはなりません。出す場合は「差分を計算できません。」に理由を続けます。シリアライズ内容が同じなら、計算不可でもダイアログは出しません。差分用の既存アセット読み込みが例外になっても、その例外だけでは保存の成否を変えません。
- 保存時に、パス上のアセットが指定した型として読めない、または別の型であるときは、保存は失敗です。パイプラインの状態は SaveFailed で、確認ダイアログは出しません。

## 16. サンプルの期待結果

サンプルは `Samples~/MasterData` です。件数は、プロジェクト側の収集警告が 0 件（同じ型のアセットが 2 件以上でない、同名の別型がない）のときの値です。`items_many_errors.json` の型不一致は含めていません。型不一致はインポートの `Succeeded` を false にしないため、別ファイルには分けていません。

| JSON | 前提 | 状態 | エラー | 警告 | 主な種類 |
| --- | --- | --- | --- | --- | --- |
| items.json | なし | 検証なら検証済み。保存するなら保存成功 | 0 | 0 | なし |
| quests.json | `items.json` の内容で作った `ItemMaster` アセットがある | 検証失敗（保存しない） | 1 | 0 | 参照切れ 1（Q002 の 999） |
| quests.json | `ItemMaster` アセットが無い | 検証失敗（保存しない） | 1 | 0 | 参照先なし 1。参照切れは出ない |
| items_many_errors.json | 対象は `ItemMaster`。JSON はパースできる | 検証失敗（保存しない）。重複と必須の検証は走る | 18 | 12 | 下表 |

`items_many_errors.json` の内訳は次です。レコードは 27 件です。

| 種類 | 重大度 | 件数 | 内容 |
| --- | --- | --- | --- |
| DuplicateId | Error | 5 | id 1 が 3 件で 2 件、id 2 が 2 件で 1 件、id 3 が 3 件で 2 件。最初の 1 件は数えない |
| MissingRequiredField | Error | 13 | `name` のキーが無い 6 件と、空または空白だけの `name` 7 件 |
| MissingField | Warning | 6 | `name` のキーが無い 6 件。同じレコードの必須エラーと両方出る |
| UnknownField | Warning | 6 | キー `note` が 6 件 |

合計はエラー 18、警告 12、問題 30 です。
