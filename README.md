# IMUControlledDigitalContentBase

## Unity version
Unity 2022.2.11f1

## 事前に必要な設定
### System.IO.Port を使用できるようにする
#### Visual Studio
①Visual Studio の
ツール > NuGet パッケージ マネージャー >
ソリューションの NuGet パッケージの管理
を選択。

②参照タブ の検索で、System.IO.Ports を入力し、出てきた、System.IO.Ports を選択。

③インストール ボタンを押す。

#### Unity Editor
④メニューのEdit > Project Settings > Player Other Settings 
Configuration > Api Compatibility Level を 
.NET Standard 2.0 → .NET Framework に変更

## 使用しているUnityPackage
Cinemachine
https://docs.unity3d.com/ja/2022.2/Manual/com.unity.cinemachine.html


