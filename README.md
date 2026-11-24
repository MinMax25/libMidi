# libMidi

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/MinMax25/libMidi/blob/master/LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/)

Standard MIDI File（SMF）を読み込み、解析・編集・変換・保存するための .NET ライブラリです。

公開済みの v2.0.0 は `net10.0-windows7.0` を対象としています。この開発ツリーは OS 固有のサフィックスを持たない `net10.0` を対象としています。

## 主な機能

- SMF Format 0 / Format 1 の読み込みと書き出し
- Format 0 / Format 1 間の変換
- チャンネル別、音色別、マルチティンバー単位へのトラック分割
- チャンネル、メタイベント、システムエクスクルーシブ（SysEx）のメッセージモデル
- テンポマップ、拍子、使用チャンネル、音源規格（GM / GM2 / GS / XG）、ドラムチャンネルの解析
- イベントの追加・削除・フィルタリング
- ヤマハ XF のインフォメーションヘッダー、カラオケ、スタイル情報の解析
- XF カラオケ歌詞の SRT / テキスト出力

## 動作環境

- ビルドには .NET 10 SDK、実行には .NET 10 Runtime が必要です。
- 開発版のターゲットフレームワークは `net10.0` です。現段階では Windows x64 で検証しています。

## ビルド

```console
dotnet build libMidi.sln
```

別のプロジェクトから利用する場合は、`libMidi.csproj` へのプロジェクト参照を追加します。

```console
dotnet add <利用側プロジェクト> reference <libMidi.csprojへのパス>
```

## 基本的な使い方

### MIDI ファイルを読み込む

```csharp
using libMidi.Messages;
using libMidi.SMF;

MidiData midi = SMFLoader.Load("input.mid");

Console.WriteLine(midi.Format);
Console.WriteLine($"Tracks: {midi.NumberOfTrack}");
Console.WriteLine($"Division: {midi.Division}");

foreach (MidiEvent<NoteOn> note in midi.GetAllEvents<NoteOn>())
{
    Console.WriteLine($"tick={note.AbsoluteTick}, ch={note.Channel}, note={note.Message.Pitch}");
}
```

### 変換して保存する

```csharp
using libMidi.SMF;
using libMidi.SMF.enums;

MidiData source = SMFLoader.Load("input.mid");
var converted = new MidiData();

SMFConverter.Convert(ConvertType.Format0, source, converted);

converted.FilePath = "output.mid";
SMFConverter.SaveMidiData(converted, SMFEncode.UTF8, filter: false);
```

`ConvertType` には次の値があります。

- `Format0`: 全トラックを統合して Format 0 に変換
- `Format1`: Format 0 をチャンネル別トラックに分割（Format 1 はコピー）
- `Instrument`: 音色単位でトラックを分割
- `MultiTimber`: 複数チャンネルを含むトラックをチャンネル単位に分割

`SaveMidiData` は `MidiData.FilePath` に書き出します。既定では有効なフィルターを適用するため、全イベントをそのまま保存したい場合は `filter: false` を指定してください。文字列イベントの出力エンコーディングは `SMFEncode.UTF8` または `SMFEncode.SJIS` から選択できます。

### 設定ファイルと音色定義の配置

開発版では、利用アプリ側でコードページプロバイダーを登録せずに CP932 の読み書きができます。

設定先の既定値はアプリケーションのベースディレクトリです。別の書き込み可能なディレクトリを使う場合は、MIDI を読み込んだり `SMFConverter.Def` や `InstMap.Map` にアクセスしたりする前に、`ConverterSetting.Root` に指定してください。

- 設定ファイルは Root 配下の `Config/Config.json` に保存します。
- 音色定義の `GM.json`、`GS.json`、`XG.json` は Root 配下の `Config/Instrument/` に置きます。`InstMap.Initialize(path)` で別の音色定義ディレクトリも指定できます。

## ライセンス

[MIT License](https://github.com/MinMax25/libMidi/blob/master/LICENSE.txt)
