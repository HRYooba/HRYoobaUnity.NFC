# HRYoobaUnity.NFC
## 1.インストール
ProjectSetting/PackageManagerから以下のScopeRegistriesを設定
- Name: `package.openupm.com`
- URL: `https://package.openupm.com`
- Scope(s):
  - `com.hryooba`
  - `com.cysharp`
  - `org.nuget`

## 2.外部ライブラリ
このプロジェクトは[PCSC](https://github.com/danm-de/pcsc-sharp), [Simple Ndef Parser](https://office-fun.com/https-office-fun-com-techmemo-csharp-nfcreading-practice06-ndefclasslib/)を使用しており、該当部分にはオリジナルのライセンスが適用されています。  
詳細についてはファイルを参照してください。
