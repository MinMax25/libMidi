# libMidi

SMF（Standard MIDI File）用に開発された .NET ライブラリです。
MIDI チャンネル、デバイスピッチ、ドラムマッピングなどの処理を簡単に行えます。

主な機能：
- MIDI デバイスのピッチ管理
- ドラムマッピングサポート
- SMF データ処理ユーティリティ

プロジェクト構成例：
- libMidi/
  - SMF/
    - DevicePitch.cs
    - DevicePitchList.cs
    - DevicePitchMap.cs
    - DrumPitch.cs
  - libMidi.csproj

注意: 一部ファイルはビルド対象から除外されています（.csproj の Remove 参照）

インストール：
1. NuGet パッケージとしてインストール：
   dotnet add package libMidi
2. または Visual Studio の [NuGet パッケージの管理] から「libMidi」を検索してインストール

使用例：
using libMidi.SMF;
var pitchList = new DevicePitchList();
pitchList.Add(new DevicePitch(0, 60));
var kick = new DrumPitch(36);

開発環境：
- .NET SDK 8.0 以上
- Windows 7 以降

ビルド：
dotnet build -c Release

ライセンス：MIT

作者：Min Max
GitHub：https://github.com/MinMax25/libMidi
