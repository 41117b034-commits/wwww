# 第三章交談、武器提示與出發確認

2026-10-13 使用者五張截圖的修改；仍使用原 `Assets/Scenes/第三章.unity`。這是既有第一部分及駐在所的調整，不是第 4 批。

## 本輪行為

- 開場由三輪改為兩輪有效玩家輸入與巴萬回覆；第一輪接話，第二輪收束。空白、請求中重複送出不計數，第二輪讀完後即可領刀。
- 領刀、領步槍時，在武器上方顯示黑框黃色小箭頭並輕微上下浮動；目標移出畫面時，箭頭移到邊緣指回目標方向。取得武器或跳段立即隱藏。同一共用字幕 Canvas，無額外碰撞物。
- 山路改以已儲存的土路 ribbon 網格外緣判定可走範圍，預留玩家半徑加 0.06m，不再使用原左右各 3m 的寬走廊。部落空地依可見橢圓保留活動空間。同伴落後等候規則保留。
- **拿刀後至抵達駐在所前的所有預寫台詞、停留節點與路線均未修改。** 僅提出「拿刀後一句同行提示 → 短路程 → 一句壓低聲音 → 駐在所」的討論方案，尚待使用者决定。
- 領槍後回巴萬身旁按 E，開啟自由文字 LLM 交談，詢問是否準備好出發。READY 接既有出發／短交戰；NOT_READY 接莫那遣返；UNCLEAR 保持交談並澄清。
- 沿用原免費雲端／本機對話傳輸，不改金鑰、模型或付費設定。每次回答均嘗試模型，限制三種意圖，嚴格解析格式；明確日常回答有決定性的備援。「不好」不被「好」覆蓋，疑問、矛盾、假設、引用及不熟悉的文字不在离線時猜測。模型不能任意完成任務、寫存檔或執行指令。
- 請求逾時或格式無效時顯示備援提示；明確肯定／否定仍可接續，不明確仍需澄清。輸入中鎖定重複送出，中文 IME 確認字不直接送出。跳段取消正在等候的請求，延遲回覆不能推進其他區域。

## 莫那遣返演出

莫那走近玩家、轉正面，使用本章獨立的憤怒表情與手勢覆寫，不修改第二章共用演出。以下均是本作編寫台詞，**不是莫那魯道歷史原話**：

1. 「槍都拿了，現在才說沒準備好？抬起頭，看著我！」
2. 「同行的人要把性命交在彼此手上。你連自己的心都沒定下來，憑什麼要他們放心把背後交給你！」
3. 「今天你不准跟來。把槍留下，回家休息！想清楚自己能承擔什麼，再來見我。」

最後一句完成，收回步槍狀態，停留、淡黑，Editor 自行停止 Play Mode，不進後續劇情。已成立的遣返不能用 F6／F7／F8 跳走；獨立程式沒有 Play Mode，改顯示結束卡及原重玩／返回選單按鈕。未寫入第三章完成或比胡生死結果。

Gaya 是祖訓／生活規範，不能把人稱為「一個 Gaya」；Seediq Bale 才有「真正的人」之意，但不能把整體文化價值簡化為是否參戰。所以本段針對同伴責任斥責，不使用「不配稱為 Gaya」，也不宣稱拒絕戰鬥等於違反祖訓。依據：伊婉．貝林〈[賽德克．巴萊的文化意涵](https://www.taiwananthro.org.tw/post/2uTBPOJLmCiRLzpQQZsQnfKDnyV)〉，臺灣人類學與民族學學會。

## 驗證紀錄

本輪證據與原檔備份：`_CodexBackups/chapter3_guidance_ready_20261013/`。原專案 Editor 驗證使用 Input System 的 E／F6／F7／F8 事件和實際 CharacterController 移動，字幕時間在測試中縮短；不等於真人全段鍵鼠、實體 VR 或建置版驗收。

最終結果與已檢視畫面於本輪驗證完成後記在此節。早期 `diagnostic-ready-first.json` 是首次長回答未分類成功的診斷；已補明確完整句備援及前往霧社的背景，不能將此早期報告当作最終通過。

最終檢查：test-village 11 項、test-road 24 項、test-ready 22 項、test-refuse 23 項、test-cancel 19 項、test-offline 21 項、test-refuse-offline 23 項均 completed=true、errors=[]、warnings=[]（含各輪重複的入口／守門檢查）。test-road 含七段山路左右共 14 個方向的實體移動阻擋，test-village 走完原路程到駐在所，test-ready 自駐在所走到短交戰；否定兩輪皆讀到最終遣返台詞後由正式程式停止 Play，returned-to-edit.txt 記錄 playing=False。取消測試驗證 F8 後回覆不會推進學校，再 F6 重入能重設。

實際服務：開場收到一個真正回覆，另一次備援；準備好長句收到真正回覆並辨識 Ready，其他請求有備援。強制逾時測試以 runtime replyTimeout=.01 模擬回覆失敗，不改雲端或本機設定。不是每次連線都成功，也不代表任意自由句子的語意準確率。早期 ready 測試因完整句備援不足而逾時，保留 diagnostic-ready-first.json；已修正後 test-ready 通過。箭頭初版缺 CanvasRenderer，已補上，最後 knife-arrow.png / rifle-arrow.png 已確認渲染。最後安全站位、嘴型計時及全形分隔符的小修後重測 test-refuse-offline、test-road；其餘功能報告來自之前同輪實作，不虛稱全套再次重跑。

停止後 audit.txt：dirty=False、playing=False、compileFailed=False、missingScripts=0、missingMeshes=0。protected-after.json 核對第一／第二／第三章場景、EditorBuildSettings、Chapter2SpeechFace 全部不變；trail-dialogue-unchanged.txt 比對拿刀後至駐在所前程式段落完全相同。臨時 probe 移出 Assets 後另做編譯檢查。既有 OpenXR runtime unavailable 訊息發生在頭戴裝置未連接的啟停階段，不把 reports.errors=[] 解釋為整份 Editor.log 毫無訊息。
