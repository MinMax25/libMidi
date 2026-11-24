# libMidi 回帰テスト

更新者：Codex
最終更新日時：2026-11-24 13:47

## 実行

Windows x64、.NET SDK 10.0.404、.NET Runtime 10.0.15、Release / Any CPU で基準を採取した。詳細は [採取環境](libMidi.Tests/Baseline/environment.json) を参照。

リポジトリルートで実行する。

```powershell
dotnet restore libMidi.sln
dotnet build libMidi.sln -c Release --no-restore
dotnet test libMidi.sln -c Release --no-build --no-restore
dotnet build libMidi.sln -c Release -p:Platform=x64
dotnet test libMidi.sln -c Release -p:Platform=x64 --no-build --no-restore
dotnet test libMidi.sln -c Release --no-build --no-restore --filter Kind=Culture
```

テストは全44件。既存17件の内訳は通常回帰11件、意図的な変更3件（T11・T13・T14）、既知挙動3件。カルチャ検証として、`ja-JP`、`en-US`、`de-DE` × T02、T03、T06、T07、T08、T09、T10、T11、T13 の27件を実行する。

プローブはケースごとに専用の一時ディレクトリと別プロセスで実行し、必要なカルチャを `--culture=<name>` で明示する。既定は InvariantCulture。カルチャ指定時も従来の意味・数値・イベントと MIDI、SRT、歌詞のバイト比較を適用する。Windows x64 では比較対象にカルチャによる差はなかった。T13 はコードページをグローバル登録せず、T09 の保存と再読込は別プロセスで実行する。

## GitHub Actions

[CI ワークフロー](../.github/workflows/ci.yml) は push、pull_request、workflow_dispatch で起動する。[標準ランナー一覧](https://docs.github.com/en/actions/reference/runners/github-hosted-runners) の Windows・Linux・macOS を x64 と Arm64 の計6ジョブで構成し、それぞれ Release / Any CPU で検証する。SDK は [global.json](../global.json) の .NET 10.0.404 に固定する。

各ジョブでは SDK 情報、復元・ビルド・テストのログ、TRX、テストホストとプローブの OS・CPU・ランタイム・カルチャを、ジョブ別の artifact に保存する。テストは直前のビルド結果を使い、T14 では nupkg 内の製品 DLL をそのビルドの DLL と比較する。

環境をローカルでも指定する場合は、`LIBMIDI_EXPECTED_OS`（`Windows` / `Linux` / `macOS`）、`LIBMIDI_EXPECTED_ARCH`（`x64` / `arm64`）を設定する。OS とプロセスの実アーキテクチャを確認し、プローブの CPU がテストホストと異なっても失敗する。作成済みディレクトリ内のファイルを `LIBMIDI_ENVIRONMENT_LOG` に指定すると、環境ログを JSON Lines 形式で保存できる。

## 基準の再採取

旧版の Windows 基準を調査するときに PowerShell 7 で実行する。CI と通常のテストは既存の基準データを読み取る。Git オブジェクトとしてコミット `2ad182a63eabd6d56a6043697c4afd59e846a446`（`v2.0.0`）が必要。

```powershell
./tests/Capture-Baseline.ps1 -OutputDirectory bin/baseline-review
```

スクリプトは基準コミットを `bin/` 内の一時領域へ展開してリリースビルドし、その DLL を参照して観測結果を採取する。指定先は新規ディレクトリのみ使用できる。更新を採用する場合は、新規採取結果を [Baseline](libMidi.Tests/Baseline/) と比較してレビューする。通常のテスト実行は基準を読み取る。

入力の SMF は [Fixtures.cs](libMidi.Tests/Fixtures.cs) と [BehaviorCases.cs](libMidi.Tests/BehaviorCases.cs) の固定16進データから組み立てる。JSON はトラック順、イベント順、tick、チャンネル、メッセージ型、メッセージバイト、文字列や音色の観測結果を記録する。JSON 文書自体の改行差を許容し、JSON 内の文字列は比較対象に含める。MIDI、SRT、歌詞テキストはバイト単位で比較する。SRT と歌詞の Windows CRLF は Git でも保持する。

T12 はビルド済み基準 DLL から公開型、基底型、インターフェイス、公開・派生型向けメンバー、record の生成メンバー、型制約、アクセサー、enum 数値、任意引数、Obsolete 情報を収集し、序数順に保存する。TFM と OS 属性は T12 の API 一覧から除く。T14 は nupkg の ID、ライブラリ配置、依存グループ、および DLL がそのビルドの成果物と同一であることを確認する。パッケージの版はそれぞれのビルド済み DLL の版と照合する。Step 3 の T14 は TFM の意図的な差分を比較時に反映し、README のバイト一致とテスト資産の非混入も確認する。

## ケース一覧

| ID | 観測内容 |
| --- | --- |
| T01 | Format 0 / 1、Division、トラック、イベント順と値 |
| T02 | フィルターの有無による MIDI バイト列と再読込 |
| T03 | Format 0 / 1 の相互変換と同形式コピー |
| T04 | Bank MSB / LSB、Program Change、複数チャンネル、ドラムを含む Instrument / MultiTimber 分割 |
| T05 | Note On / Off、CC、Program Change、Polyphonic Key Pressure、AfterTouch、Pitch Bend、SysEx、主要メタ、EOT、running status |
| T06 | 120 → 60 BPM、4/4 → 3/8、小節・拍・tick、実時間、VLQ 127 / 128 / 16383 / 16384。BPM の許容誤差は 0.0001、実時間は TimeSpan の1 tick |
| T07 | 固定バイトで ASCII、UTF-8、CP932、拡張文字、曖昧・空・不正データ、CP932 非対応文字を検証 |
| T08 | GM・GS の既定ディレクトリ、XG の明示ディレクトリ、音色名、欠落時の挙動 |
| T09 | 既定 Root、設定値とフィルター、明示 Root への保存と別プロセスによる再読込 |
| T10 | XFIH / XFKM、XF 変換、SRT の時刻とコメント除去、SRT・歌詞の書き出し |
| T11 | 日本語と空白を含むパス、通常のトラック名と区切り文字を含むトラック名の分割保存 |
| T12 | 公開 API のスナップショット |
| T13 | プロバイダー未登録の別プロセスで UTF-8 / CP932 を読み込み、CP932 保存のバイト列と再読込を確認。処理前後ともグローバル登録がないことを確認 |
| T14 | nupkg の TFM、依存関係、ビルド済み DLL とのバイト一致 |

## 既知の挙動と検証範囲

| 分類 | 観測結果 |
| --- | --- |
| v2.0.0 の記録 | T13 では UTF-8 / CP932 のデコードと CP932 保存が `NotSupportedException`、ファイル読込は空トラック。Step 3 のテストは登録なしでの読込と保存の成功を確認する |
| 既存不具合 | `MetaText.Length` は空データと単独の `80` で `IndexOutOfRangeException`。`SequencerSpecific.Length` は `80 01` で 6 を返し、実際の出力は 5 バイト |
| 既存不具合 | 既定の定義を読込済みの `InstMap` に同じ定義を再初期化すると `ArgumentException` |
| 未実装 | `TuneRequest.GetByte()` と `MidiStart.GetByte()` は `NotImplementedException` |
| v2.0.0 の記録 | トラック名 `A/B` は `#0 A/B.mid` として扱われた。Step 3 は `#0 A_B.mid` を指定フォルダー直下に出力する。異なる名前が同じ文字列に置換されても同じ保存内ではトラック番号で区別する |
| 未検証 | ビッグエンディアンでのテンポ解析と Windows x64 以外の OS / アーキテクチャ |
| 対応範囲外 | 既存ファイルの上書き、予約名、末尾の空白・ピリオド、長さ、Unicode 正規化、ケース差、シンボリックリンクに関する包括的な検証と対策 |

`SaveSetMidiData(filename: ...)` は明示した名前に拡張子や用途別の接尾辞を付けて `folderPath` と結合する。既存コードは名前にパス区切りを含められ、ルート付きパスでは `Path.Combine` の規則で `folderPath` の外を選べる場合がある。呼び出し側との互換性を確認したうえで、名前をファイル名1要素に制限するのか、相対パスを明示的に許し出力ルート内に制限するのかを決める。Step 3 の安全化はトラック由来の分割保存に適用する。
