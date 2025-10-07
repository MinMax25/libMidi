# libMidi

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet)](https://dotnet.microsoft.com/)

SMF（Standard MIDI File）用に開発された .NET ライブラリです。  
MIDI チャンネル、デバイスピッチ、ドラムマッピングなどの処理を簡単に行えます。

---

## 🌟 主な機能

- MIDI デバイスのピッチ管理
- ドラムマッピングサポート
- SMF データ処理ユーティリティ

---

## 🚀 インストール

NuGet パッケージとして利用可能です。

```bash
dotnet add package libMidi

## 使用例：
using libMidi.SMF;
var pitchList = new DevicePitchList();
pitchList.Add(new DevicePitch(0, 60));
var kick = new DrumPitch(36);

## 開発環境：
- .NET SDK 8.0 以上
- Windows 7 以降

## ビルド：
dotnet build -c Release

## ライセンス：MIT

作者：Min Max
GitHub：https://github.com/MinMax25/libMidi
