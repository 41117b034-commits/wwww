# 對話字幕遮擋與會場射擊反應

日期：2026-11-06。本次接續使用者回報「NPC 遮住字幕」與「進入達多會場仍延遲／無反應」，以目前正式專案重新重現，不把上一輪輸入測試當作本輪畫面效能證據。

## 字幕

第二、三章使用距離鏡頭 0.5 m 的 Screen Space Camera Canvas，第一章劇情字幕使用 World Space Canvas；提高 sorting order 仍無法避開較近 NPC 的深度遮擋。

新增 `GameDialogueOverlay` / `GameDialogueOverlayGraphic` 與 Resources 內兩個 UI shader，讓字幕文字、底板及同一對話介面的圖形最後繪製，不被場景深度擋住。沿用原 Canvas 模式、位置、字型、台詞、換行、透明度、遮罩與互動；新增的對話圖形和 TMP 字型子網格也會套用。材質按圖形快取，沒有每幀搜尋全場景或建立新材質。

實際覆蓋入口：第一章 `Chapter1StorySubtitleBridge`、`Chapter1DialogueUI`、`Chapter1ChoiceUI`，以及現場舊 `PoliceSubtitleCanvas_Auto`；第二章 `Chapter2Presentation`；第三章所有區域、任務和交談共用的 `Chapter3Presentation`。第一章 OnGUI 後備文字本來就在畫面層。模擬器與睜眼遮罩不是對話，沒有更改。未逐句重寫劇情或呼叫真實 LLM；本次既有台詞字串無變更。

## 會場

主因是大量人物沒有實際套用原有遠距模型：

- 十名埋伏族人的原骨架複本沒有 Chapter2Actor，舊背景 catalog 又只有烘焙版本，兩套模型切換皆漏掉。
- 埋伏族人及兩位替換跑步的學童起初不啟用；GetComponentInParent 預設不包含停用的父物件，有限的初始搜尋因此漏掉他們。
- 學童換裝會建立新網格，原有依來源識別的模型切換沒有對應這些新網格。

現在搜尋包含未啟用人物，新增三種骨架相容的會場遠距族人網格，原始約 100 萬頂點降至 77,300／80,074／87,453。使用專案既有、固定版本 `UnityMeshSimplifier` 離線產生；保留骨架索引、權重、UV、材質分組與邊界。換裝的遠距網格套用同一服裝分區，並按原角色局部方向和身體範圍建立共用的 15 骨架姿勢，引用歸零後釋放。

背景人物距離超過 5 m 切遠距，進入 3.5 m 恢復原網格，避免來回閃切。保留原十名族人、二十位觀眾、碰撞與動作；沒有刪除角色來提高 FPS，沒有更改一般步速、導航、撤退、救援或戰鬥規則。本輪沒有更改 Chapter3Weapons，上一輪的單發裝填和輸入緩衝仍保留。

## 當次驗證

證據：`_CodexBackups/dialogue_fire_response_20261106/`。正式第一、二、三章 Editor Play Mode；最終三份報告共 71 項觀測檢查，包含重複人物檢查：

| 報告 | 項數 | 範圍 |
| --- | ---: | --- |
| report-ui.json | 9 | 三章近距不透明遮擋物前後像素比對、實際圖形材質、第三章世界／桌面模式切換、貼近 NPC 字幕畫面 |
| report-input.json | 37 | 山路與達多會場短按、同次輸入更新按下再放開、R 裝填、400 ms 人工卡頓跨過緩衝、焦點取消、刀連揮、實際警察命中、撤退十人、倒地重試及 F8 |
| report-native-final.json | 25 | Windows 視窗八次滑鼠點擊、十名族人與十名學童、三種族人近遠網格恢復、原骨架／材質／碰撞保留 |

效能比較為本機 Editor Full HD Game、相同玩家位置與視線、正常動作中的會場、各取約 6 秒。原版 `report-diagnostic-before.json` 影格中位數 109.20 ms（約 9.2 FPS），最終為 37.19 ms（約 26.9 FPS）；第 95 百分位從 118.97 ms 降到 45.21 ms。這是指定視角的測量，不是所有位置或獨立版的最低／保證 FPS。部分 Unity Recorder 的工作執行緒數值出現無效負值，未用來作效能結論。

八次 Windows 滑鼠點擊全部 raw=8、polled=8、Shots=8，均在同一個 Unity frame 接收與發射。第一發收到 raw event 後到測試觀察 Shots 約 88 ms，後七發約 3.5–6.1 ms；這不等於滑鼠硬體到螢幕的完整延遲。測試固定觀看方向並由驅動器在復位後請求正常裝填，保留人群動畫與碰撞。工具送出的原生 R 未出現在 Unity raw keyboard event，故不宣稱通過原生鍵盤裝填；R 與快速按放由另外的 Input System 測試確認。未新增自動裝填遊戲機制。

字幕像素測試在鏡頭前 0.2 m 放不透明物體：舊材質文字區白色像素皆為 0，修正後第三章 1,102、第二章 1,264、第一章 988；已查看對應圖片。早期測試曾忘記正式場景 Awake 安裝、受到世界模式自動回切及片頭影片／黑色過場遮罩干擾，診斷保留，沒有用那些畫面當最終通過證據。

上述為 Editor 驅動與部分原生滑鼠驗證；包含定位、固定視線、停火、Hit(100)、人工卡頓和 UI 過場暫停 fixture，沒有完成真人整章鍵鼠、實體 VR、獨立 exe、真實 LLM 或本輪兩個完整救援結局驗收。VR 僅檢查 Canvas 模式／材質；未宣稱頭戴裝置測試通過。

正式三個場景、EditorBuildSettings、ProjectSettings 與修改前 SHA256 相同。測試來源和 meta 收尾移至證據目錄的 verification-tools，移出後重新編譯；最終狀態和來源雜湊見 final-state.json。未 commit／push。
