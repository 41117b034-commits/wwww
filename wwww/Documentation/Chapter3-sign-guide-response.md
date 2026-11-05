# 第三章路牌、會合提示與操作效能

更新：2026-11-05。依使用者兩張第三章截圖及開槍／揮刀延遲回饋修正，沿用正式 `Assets/Scenes/第三章.unity`，並參考固定劇情文件的前往霧社、學校會合與交戰段落。前一輪第一至第三章配音停頓修正保留。

## 畫面與操作

- 駐在所出口「霧社 →」沿實際連接道路校正朝向：執行時 yaw 35° 改為 0°，印刷箭頭的 local +X 對齊當地向東路段。原路牌與碰撞體一起移到路中心線外 3.1 m，位置 (-9.8, 0, -94.9)，文字保留。
- 保留地面黃色箭頭，另加畫面內的黃色方向箭頭、黑色描邊及「達多／距離」底板。達多在畫面外或身後時，提示限制在畫面邊緣；看向他時顯示於目標上方。會合對話開始、離校或重置時隱藏／重新使用同一提示，不轉動玩家鏡頭。
- 就緒時點擊照常立即射擊。冷卻或裝填剩下 0.25 秒內的點擊會短暫保留，動作完成後執行一次；過早的裝填點擊不保留。射擊後立刻按 R 會等槍械復位再裝填。獵刀可按住左鍵連揮，放開即停止接續；步槍仍需每發重新點擊。Esc、失焦、換區、重置及扶行清除待執行輸入。
- 保留刀 0.68 秒間隔、槍 0.48 秒復位、2.6 秒單發裝填、彈藥及命中規則。這些必要動作時間仍存在。

## 效能修改

`Chapter3ViewArm` 預先計算前臂變形權重；相對持握姿勢變動才更新網格，使用解析法線，避免每幀遍歷所有三角形重算法線。手掌、原頂點數及變形位置公式保留。

`Chapter3ActorDetail` 僅在第三章安裝。原生人物及 NPC 步槍超過 5 m 使用離線簡化網格，回到 3.5 m 內恢復原網格，保留人物骨架、材質、臉部元件、碰撞、命中區域及動作。區域／戰鬥階段改變時有限次發現新角色；清除已銷毀的綁定，停用時恢復自己的原參照。既有背景群眾 LOD 繼續由原元件管理。

警察執行時會換成帶三個 grip／sole blendshape 的網格，不能只匹配 FBX 原網格。共用 `Chapter1IncidentRig` 僅新增唯讀 `TryGetGripMeshes` 查詢；第三章使用相容三個 blendshape 的衍生網格並保留其權重。第一、二章不安裝本次 LOD。

資產位於 `Assets/Chapter3/Resources/Chapter3/ActorDetailCatalog.asset` 與 `ActorDetailMeshes/`。七種原生人物，身體約 18% 三角形目標比例；其中巴萬及學童重用既有衍生資產。新產五份人物身體、一份警察握持版本與一份 NPC 步槍。警察 767,047 → 157,242 頂點；NPC 步槍 1,334,410 → 129,209 頂點。步槍遠景允許合併 UV 接縫，靠近仍使用原模型。未修改來源 FBX、正式場景、人物比例或動線。

離線生成沿用 [UnityMeshSimplifier 固定版本](https://github.com/Whinarn/UnityMeshSimplifier/tree/53fdb3122645bcd3ad2c258235200dff3dfbaa9b)，MIT 原始碼／授權在 `_CodexBackups/chapter3_hands_fire_20261019/verification-tools/UnityMeshSimplifier/`；本輪生成與驗證來源封存於本輪證據目錄。沒有新增第三方執行時 DLL。

曾比較 [Unity GPU skinning／meshDeformation 選項](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerSettings-gpuSkinning.html)，早期未完成 LOD 時在這台 Intel Iris Xe 上反而較慢，已還原 CPU 設定。畫面外蒙皮與陰影修改也僅作診斷，最終未採用；全域品質、URP 資產及 ProjectSettings 不變。

## 本輪驗證

機器為 Intel i5-1235U／Iris Xe／16 GB RAM。正式第三章、Editor 前景 Game View 1920×1080、vSync 0、無幀率上限；同次 Play Mode 固定視角，切換本次人物／NPC 步槍 LOD 作對照，每段 5 秒。第一人稱手部優化在兩側都啟用。所有取樣幀皆有焦點；山路敵方射擊暫停以隔離量測，人物及場景仍繪製、物理仍運作。數值為幀時間中位數換算 FPS，並非全章平均或獨立執行檔效能。

| 區段 | 關閉本次 LOD | 最終 LOD 開啟，兩次取樣 | 最終 p95 幀時間 |
| --- | --- | --- | --- |
| 山路交戰 | 135.98 ms，7.4 FPS | 43.34／48.65 ms，23.1／20.6 FPS | 47.86／55.36 ms |
| 校門 | 63.91 ms，15.6 FPS | 34.10／34.61 ms，29.3／28.9 FPS | 38.64／41.02 ms |

正式渲染中山路三角形統計由最初約 5,347 萬降至約 1,800 萬，包含陰影等重複繪製；不是模型資產自身三角形總數。Profiler 的 MeshSkinning.Skin 聚合 worker 時間不可當成單幀主執行緒耗時。最初完整舊版山路樣本為 127.46 ms，約 7.85 FPS；其測試開場遮罩尚在，保留作診斷而不取代上述最終同畫面對照。

- `report-functional.json`：51 項，completed=true、errors=[]。涵蓋路牌方向／碰撞、箭頭離屏／身後／轉向、會合隱藏與 F8 重入、首次點擊、R 緩衝、裝填早／晚點擊、獵刀按住／放開／末段點擊、Esc／失焦清理、手部幾何與法線、近遠模型切換及還原。使用合成 Input System 事件；首次點擊到兩個 coroutine 幀後觀察約 44.5 ms，不是實體滑鼠到螢幕延遲測量。
- `report-combat-pose.json`：22 項，completed=true、errors=[]。以真實警察碰撞射線與武器 API 實測遠距步槍擊倒、近距兩刀擊倒、倒地後顯示、材質／骨架、正常停用碰撞、達多蹲姿、重複快捷入口綁定數。敵人僅 HoldFire／AttackEnabled=false，沒有直接指定血量或命中結果。原／簡化網格的站姿、倒姿、蹲姿保守世界 bounds 差均低於 1.3 mm。
- `report-final-perf.json`：completed=true、errors=[]，六段前景效能樣本。
- `final-audit.txt`：確認正式場景無 missing script，八筆目錄來源／衍生參照、UV、法線、submesh 及 bindpose 有效。實際引擎截圖已檢視路牌、會合箭頭、前臂、人物及 NPC 步槍／倒地。

測試使用明示相機 Warp、敵方停火及快捷入口 fixture，未重跑整章真人鍵鼠、救援兩分支、實體 VR 或 Standalone。山路仍只有約 21–23 FPS，不能宣稱已穩定 30／60 FPS 或完全消除卡頓。現有證據支持模型繪製負擔及輸入拒絕共同影響體驗，不能據此判定電腦故障。

證據／修改前備份：`_CodexBackups/chapter3_response_20261105/`。早期空目錄、不可讀步槍生成及臨時 probe 編譯問題已修正；診斷產物不列為通過驗證，最終以三份 completed 報告、final-audit 與 final-state 為準。舊 exe 不包含本輪修改；未 commit／push。

收尾：三章場景及 Build Settings 四份 SHA256 不變；七份 runtime、目錄與七份新網格共十五檔雜湊記於 final-state。三份臨時工具及 meta 已移出 Assets，清理編譯成功（3.30 秒）、assembly reload 完成，Editor DLL 無本輪臨時類別。已確認停止 Play、無未存場景標記、預覽仍 0.23x；`git diff --check` 通過。
