# Graph Simulation (ver 2.1.0)

グラフ上での進行波の生存・消滅の実験を視覚化するシミュレーションプラットフォームです。直感的なグラフエディタにより、複雑なグラフを自在に構築し、動的なエージェントベースのシミュレーションを実行できます。

<p align="center">
<img width="600" alt="スクリーンショット 2026-04-10 16 47 08" src="https://github.com/user-attachments/assets/e1d0762a-e750-41f4-b7c9-a75b62edebf0" />
</p>


## 🌟 主な機能

### 1. グラフエディター
モード切り替え方式を採用した、直感的な操作感を持つエディタです。
* **ノード設置**: クリック一つでグリッドスナップに対応したノードを配置。
* **エッジ接続**: 2つのノードを選択して辺(エッジ)を形成。
* **自由な移動**: 配置済みノードのドラッグ移動（接続された辺は自動追従）。
* **スマート削除**: ノードとその付随する辺を一括削除。

### 2. シミュレーション
「波」を模したWaveAgentによる動的なシミュレーション。
* **波の分岐**: ノード到達時に隣接するすべての辺へ波が分裂・伝播。
* **波の干渉システム**: 異なる発生源（Wave ID）を持つ波が正面衝突した際、互いに消滅する物理ロジックを搭載。
* **Greenberg-Hastings Cellular Automaton対応**: セル・オートマトン的な興奮・抑制状態のシミュレート。

### 3. データ管理 & ユーザビリティ
* **セーブ/ロード機能**: 構築した複雑なグラフ構造をスロットごとに保存可能。
* **Undo/Redo**: 編集ミスを恐れず作業できる、直感的なショートカット操作（Cmd+Z / Cmd+Y）。
* **レスポンシブUI**: スタイリッシュでモダンなUIを採用。

<p align="center">
<img width="600" alt="画面収録 2026-04-10 17 22 42" src="https://github.com/user-attachments/assets/84f16566-bdbd-42da-a765-44107a7a2b95" />
</p>

## 🚀 クイックスタート

1. **グラフを作成**: `ノード設置`モードで頂点を作り、`辺設置`モードで繋ぎます。
2. **シミュレーション開始**: `スタート`をクリックし、初期地点となるノードを選択します。
3. **干渉の観察**: 複数の地点から波を発生させ、ネットワーク上で波が消滅し合う様子を観察します。

<p align="center">
<img width="600" alt="画面収録 2026-04-10 17 16 46]" src="https://github.com/user-attachments/assets/2288895b-f9d9-452c-9fe0-21ea29aa983c" />
</p>

## 🛠 技術スタック

* **Engine**: Unity 2018
* **Language**: C#
* **Architecture**: Singleton Pattern (GraphManager), Agent-based Modeling
* **Physics**: Layer-based collision filtering for optimized performance

## ⚠️ 対応OS
* **Mac OSのみ対応しています**

---
© 2026 [honmayuto02]
