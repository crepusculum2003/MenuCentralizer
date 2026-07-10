# Menu Centralizer

VRChatアバターの `VRCExpressionsMenu` 及び、各種prefabにあるメニューを、一箇所にまとめ、非破壊的にメニュー管理を一元化するプラグインです。

## これは何をするツールか

各種ギミックを多く入れるとメニューが溢れてしまうという問題があります。メニューを整理する手法はいくつかありますが、プロジェクトごとに手動で操作する必要があり、やや手間です。

本ツールは、私がメニュー整理する際に行う作業を自動化したものです。具体的には、
一元的にメニューを管理し、hierarchy上の階層構造をいじることで、メニューを自由にグループ化、順番入れ替えなど管理できるようにする土台をワンボタンで設定するツールです。

また、非破壊的ツールであり、本プラグインによって追加されたものを削除すれば元通りになります。

## 必要環境

- [Modular Avatar](https://modular-avatar.nadena.dev/ja)
- [NDMF (Non-Destructive Modular Framework)](https://ndmf.nadena.dev/)（Modular Avatarに同梱）

Modular Avatarが検出できない場合、実行時にインストールを促すダイアログが表示されます。

## 使い方
### 1. メニューをMA_Menuへ展開する

1. Hierarchy でアバタールート（`VRCAvatarDescriptor` が付いたオブジェクト）を選択
2. `GameObject > Menu Centralizer > Extract Menu to MA_Menu` を実行


### 2. 追加されたPrefabのMenu Installerのみ更新する

アバター内に後から追加した `ModularAvatarMenuInstaller` を、`MC_ROOT` の管理下（Install Target）に追加したいだけの場合に使用します。

1. アバタールートを選択
2. `GameObject > Menu Centralizer > Sync Install Targets` を実行

`MC_ROOT` の子孫ではない、かつ有効な `ModularAvatarMenuInstaller` を走査し、未登録のものだけ `ModularAvatarMenuInstallTarget` として `MC_ROOT` 直下に生成します（重複登録は自動スキップ）。

### （おまけ）3. サブメニューをワンクリックで作成する

1. Hierarchy で親にしたいオブジェクトを選択
2. `GameObject > Menu Centralizer > EASY Submenu Creation` を実行

選択オブジェクトの子として `Submenu` という名前のGameObjectが作成され、`ModularAvatarMenuItem`（`ControlType.SubMenu`、`MenuSource = Children`）コンポーネントが自動で設定されます。


## 注意点

- faceemoの更新など、hierarchy上で同名のprefabが再度生成された場合、該当するinstall targetの参照先を手動で更新する、またはinstall targetを含むGameObjectを削除->syncしてください。

- ギミック等のma対応prefabでインストールされるメニュー自体を編集したい場合は該当prefabをいじってください。現在、該当機能は開発中です。

- Modular avatar menu installerを介さずにメニューが追加されるタイプのものは本プラグインで認識されません。（スクリプトで追加する系）
- VRCFury由来のメニュー系コンポーネントは未検証の部分が多く、正しく動作しない可能性があります。toggle、Reorder Menu Itemは動作確認しました。VRCFuryのSelect Itemで表示されないのは仕様です。パスの手入力で動作します。

---

## 以降は技術的な話です。
### ビルド時の挙動（NDMFプラグイン）

`NDMFPlugin`（`com.seal.menu-centralizer`）は、MAのビルドフェーズより前（`BuildPhase.Transforming` かつ MA プラグインの前）で以下を行います。

- アバター内に `ExMenuInjector` コンポーネントが存在する場合のみ動作
- `VRCAvatarDescriptor.expressionsMenu` を、メモリ上でのみ生成した空の `VRCExpressionsMenu` に差し替え（アセットとして保存はされない）

これにより、実際のアバターアセット（元のExpressionMenu）を変更せずに、ビルド結果ではMAのMenu Installer経由の階層のみが反映されます。

`ExMenuInjector` がアタッチされたGameObjectのインスペクタには、この動作を説明するヘルプボックスが表示されます。何らかの理由でMenu InstallerとAvatar Descriptor側のメニューを共存させたい場合は、このコンポーネントを削除することで従来通りの挙動（差し替えなし）に戻せます。

### メニュー項目一覧

| メニューパス                                              | 機能                                         |
| --------------------------------------------------------- | -------------------------------------------- |
| `GameObject > Menu Centralizer > Extract Menu to MA_Menu` | ExpressionMenuをMA Menu Item階層へ差分抽出   |
| `GameObject > Menu Centralizer > Sync Install Targets`    | 既存Menu InstallerをInstall Targetとして同期 |
| `GameObject > Menu Centralizer > EASY Submenu Creation`   | サブメニュー用GameObjectをワンクリック作成   |

いずれも、Hierarchyで `VRCAvatarDescriptor` を持つオブジェクト（もしくはその子）を選択している必要があります（未選択・対象外の場合はメニューがグレーアウトされます）。

`Extract Menu to MA_Menu`の内部処理：実行すると以下が行われます。

- アバター直下に `MC_ROOT` という GameObject を作成（既にあれば再利用）
- `MC_ROOT` に以下のコンポーネントを付与
  - `ExMenuInjector`（ビルド時の空メニュー差し替えトリガー）
  - `ModularAvatarMenuInstaller`（`menuToAppend` / `installTargetMenu` は共に `null` = アバタールートメニューへ直接統合）
  - `ModularAvatarMenuGroup`
- 既存の `VRCExpressionsMenu` を再帰的に読み取り、`MC_ROOT` 以下に control ごとの GameObject（`ModularAvatarMenuItem` 付き）を生成
  - **差分更新**：同名の GameObject が既に存在する場合は新規作成せずスキップ（SubMenuの場合は子階層のみ再帰的に差分チェック）
  - SubMenuタイプの場合、`MenuSource = Children` を設定し、子GameObjectをそのままサブメニューとして扱う
- 抽出後、アバター内の他コンポーネントが持つ `ModularAvatarMenuInstaller`（`MC_ROOT` の子以外・有効なもの）を検出し、`MC_ROOT` 内に `ModularAvatarMenuInstallTarget` を自動生成して同期
- 完了後、追加数・スキップ数を記載したダイアログを表示
