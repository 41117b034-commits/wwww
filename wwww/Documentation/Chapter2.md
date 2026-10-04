# 第二章：新規定與秘密會議

固定劇情來源：`Documentation/Story/賽德克事件劇情脈絡.pdf` 第 3–5 頁。原始 PDF 副本與桌面原檔 SHA-256 相同：`93D312AF49F8740AF609BAE3EDCA7F34F7293184D61D1F2007E59AC20A2F0E6E`。

## 開啟與操作

開啟 `Assets/Scenes/第二章.unity`，按 Play。原第二章婚禮布景已備份到 `_CodexBackups/chapter2_20261004/第二章.before.unity`；場景 GUID 保留，章節選單原有連結繼續指向同一場景。

- 片頭：VideoPlayer 播放 `Assets/Chapter2/Media/Chapter2_Opening.mp4`；E、空白鍵或右手扳機可略過。缺片或解碼失敗時顯示背景劇情字幕，不會卡死。
- 移動：WASD；按住滑鼠右鍵環顧。VR 使用左搖桿移動、右搖桿分段轉向，保留頭部追蹤。
- 跟隨族人到聖地，在巨木前選擇 1 保護／2 砍伐，也可點選畫面按鈕。
- 砍伐：面向樹幹，游標進入綠色區域時按 E／空白鍵／右手扳機；五次有效落斧完成。錯誤落斧損失 20% 完整度；歸零會提示重新練習，再開始本輪砍伐。遠離樹幹、背對樹幹或按得過快不計入。
- 夜晚會議：聽完莫那魯道與六位領袖的發言，按 1 支持／2 拒絕。VR 右手舉過頭部持續約 0.85 秒可支持，左手主按鈕可拒絕；右手主按鈕亦可支持。
- 結尾：全黑顯示「決戰的時刻，將至。」；Editor 自動結束 Play Mode；建置版留在完成畫面，可回章節選單。

## 劇情對應

| PDF 要求 | 本次實作 |
| --- | --- |
| 新的搬運規定與聖地伐木背景 | 六段森林鏡頭與字幕的 48 秒可替換導入影片 |
| 早晨森林、巨木、地標、自由環顧 | 獨立森林谷地、巨木與樹根、石塊地標、林地地面、晨光與霧 |
| 跟隨族人前往聖地 | 三名族人引路，玩家落後時等待 |
| 保護巨樹的後果 | 警察威脅，黑幕槍聲，一名族人倒下；保留巨木 |
| 砍伐與木材完整度 | 有效距離、面向檢查、節奏區間、完整度、失敗重試、樹倒動畫 |
| 夜晚森林火堆會議 | 淡黑轉夜景，莫那魯道、六名領袖與兩名保守派族人 |
| 支持／拒絕起義 | 支持時談自由生活；拒絕時爭執並有保守派離場 |
| 站起演說與拉遠結尾 | 桌面鏡頭近景／拉遠，VR 保留頭部視角；黑幕結語 |

莫那魯道的兩句關鍵發言與最後黑幕文字沿用 PDF。其他族人、警察與六社發言是為了銜接玩法而新增的劇情台詞，不應引用為史實原話。拒絕分支保留玩家已作出的拒絕，不再強迫玩家點頭同意。

## 美術來源與目前替代內容

- 新下載：Poly Haven 的 Forest Floor 1K 貼圖與 Tree Stump 01 1K FBX／貼圖，CC0。授權、下載 URL、大小與 SHA-256 保留在 `Assets/Chapter2/Environment/PolyHaven/`。
- 素材推薦：<https://polyhaven.com/collections/pine_forest>，可挑選需要的森林素材；本次未整包匯入。另有 Unity Asset Store 的 Environment Pack: Free Forest Sample，<https://assetstore.unity.com/packages/3d/vegetation/environment-pack-free-forest-sample-168396>，本次未下載或購買。
- 既有素材：Hipernt Pine Pack、綠樹、Rock_pack、人物與步槍。第二章轉用 URP 的材質副本保存在 `Assets/Chapter2/Materials`，不修改第一章原材質。
- 巨木樹幹、盆地、斧頭、營火與節奏 UI 為本次建立。場景是劇情美術，並非西仔希克的測繪或史實復原。
- 開場影片是 Unity 場景鏡頭搭配字幕的初版，不是歷史紀錄片；目前沒有正式人物配音或完整搬木表演。可在 `Chapter2_NewRulesAndSecretCouncil` 的 Opening Film 欄位更換成正式影片。
- 莫那魯道及領袖暫用既有族人模型，並非其外貌復原；人物動作為可玩流程用的程序姿勢。
- 林地鳥鳴／夜間蟲鳴、砍木聲與槍聲是本次合成的替代音效；營火沿用專案既有聲音。

## 程式與維護

`Assets/Chapter2/Chapter2Controller.cs` 為章節狀態機；`Chapter2Player.cs` 管理桌面／XR 輸入；`Chapter2Presentation.cs` 管理 UI；`Chapter2Actor.cs` 重用既有動作骨架。

完成結果寫入獨立的 `WusheEvent.Chapter2.Result` PlayerPrefs JSON，包含兩次選擇、有效與錯誤砍伐次數、木材完整度、一名傷亡的保護分支結果與完成旗標。第三章尚未接入此紀錄。

編輯器工具 `Tools > Chapter 2 > Build Forest Chapter` 會重建第二章，請先保存自己的場景調整並另存副本；不是日常播放需要的步驟。`Render Opening Film` 會重新輸出 48 秒片頭。正式遊戲不需要這些編輯器工具或網路連線。

本次驗證紀錄與未完成事項見根目錄 `PROJECT_CONTEXT.md` 的第二章段落；不要將 Editor 流程測試當成實體 VR 驗證。
