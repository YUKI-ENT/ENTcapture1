# ENTcapture

ENTcaptureは、DirectShow対応ビデオキャプチャーデバイスから動画や静止画をキャプチャするツールです。RSBaseとの連携を前提に作られています。

## 動作概要のデモ
[![紹介動画](https://img.youtube.com/vi/gKA60FDcURs/mqdefault.jpg)](https://youtu.be/49kGP-nwlqU)

## 対応環境
- Windows7、8.1、10、11 (64bit/32bit)
- メモリは4GB以上推奨
- Intel Core i5 2500 3.3GHz以上が必要
- .NET framework 4.8

## 対応キャプチャーデバイス（当方で使用実績のあるもの。これ以外もDirectShow対応であればだいたい動きます。）
- IEEE1394 DVキャプチャ（ADVC300、Twinpact110等）
- Name card scanner BC-01
- Logicool C270
- USB3HDCAP
- OMAX C-mount microscope camera等

## 機能
- 複数のキャプチャデバイスの切り替え、取込解像度の指定が可能
- RSBaseとの患者ID、患者名の同期機能
- 動画圧縮Codec：XVID、DivX、h264など利用可能
- 取込動画の簡易編集、fps変更、再圧縮機能（ffmpeg使用）
- 静止画の一部領域のみの切り取り、文字書き込み可能
- キャプチャデバイス毎にWhite balance、γ補正の適用が可能
- 記録漏れをなくすことがコンセプトですので、記録開始とともに一旦一時動画ファイルにキャプチャ内容を保存します(全録画)
- キー割り当てのできるものならフットスイッチで静止画保存が可能
- 描画、録画、フィルタ適用は非同期マルチスレッド処理とダブルバッファにしているので、スレッド数の多いCPUやメモリが多いほど安定して動作するはずです
## リンク
- 開発元・旧バージョンや説明：https://yuuki-jibika.com/software/entcapture.html

