# 成熟中文配音與第三章四角色（2026-11-04）

使用者試聽後認為上一版太年輕、有活力，接受較慢、較沉穩的樣本，並要求第三章有四種聲音。本版取代先前 Microsoft Zhiwei／Hanhan 的固定中文錄音；字幕及原族語路徑維持原內容。

## 角色

| 角色 | 神經語音 | 基本語速 | 基本音高 | 方向 |
|---|---|---|---|---|
| 巴萬／其他男性 | zh-TW-YunJheNeural | -4% | -14 Hz | 使用者接受的成熟樣本設定 |
| 莫那 | zh-CN-YunjianNeural | -7% | -8 Hz | 較沉穩、威嚴 |
| 達多 | zh-CN-YunxiNeural | -4% | -14 Hz | 俐落，但降低年輕輕快感 |
| 比胡 | zh-CN-YunyangNeural | -5% | -6 Hz | 較溫和、克制 |
| 第一章女性 | zh-TW-HsiaoChenNeural | -3% | -5 Hz | 放緩、減少尖亮感 |

四個男性角色使用四個不同語音模型，不只是同一個錄音改 pitch。巴萬是台灣中文模型，另外三位是中國大陸普通話模型，因此口音仍有差別，需由使用者試聽決定是否合適。這些是合成角色聲音，沒有複製真實人物聲紋。

`Assets/StreamingAssets/ChineseVoice/delivery.json` 是固定錄音及即時合成共同設定。按受傷／悲傷、警戒、催促、溫和關鍵字，輕微修正語速與音高；只是可重現的韻律調整，不宣稱有細緻演技或情緒模型。中文字幕與合成原句均未重寫。達多姓名先於莫那比對，避免「達多．莫那」套成父親的聲音。

## 接入與離線

`GameChineseVoice` 以「角色＋原句」快取。第三章既有 198 個文字鍵均備齊四角色錄音，涵蓋分支與預備／非實播字串；第二章序列化的莫那、達多、巴萬台詞也使用對應角色。第一章其他男性使用成熟巴萬基礎設定，女性維持女聲。

固定音檔隨 Resources 提供，完全離線可播。Windows 未命中固定句的新對話使用 bundled Python worker 連網合成，同樣依說話角色選聲。它不要求玩家自行安裝 Python；`ChineseNeuralRuntime` 驗證內附 ZIP 的 SHA256 後，在遊戲私人快取解壓，並沿用相同版本。取消或換句只停止該句所開的隱藏程序與請求，清理暫存。

線上服務未回傳音訊會短暫重試，整體請求有 22 秒限制；失敗時回到已安裝的 Windows 中文語音，避免整句沒有聲音。因此「離線且從未出現的新句」仍是舊本機聲音，不保證四種不同模型；固定劇情四男聲不受此限制。其他平台目前只提供固定錄音，沒有動態合成實作。此次不宣稱已完成獨立 exe 或 VR 驗證。

官方語音列表：[Microsoft Speech language support](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=tts)。使用的線上客戶端及參數能力：[edge-tts](https://github.com/rany2/edge-tts)。可修改的 worker、原始依賴及授權隨 `Assets/StreamingAssets/ChineseVoice/` 提供；重建腳本為 `Tools/build-neural-runtime.py`。

## 重建與證據

`Tools/natural-chinese-voice.py` 依清單重製音檔，成功記錄及參數保存在證據目錄，重跑可續作；全部成功才寫入四角色清單。音檔保留原有資源名稱與已有 meta GUID，新角色另加資源。`Tools/chinese-voice.mjs` 重新盤點文字時保留既有角色變體與來源資訊，避免覆蓋。

本輪證據目錄：`_CodexBackups/chinese_natural_voice_20261104/`。`four-characters-preview.wav` 順序為巴萬、莫那、達多、比胡，同句比較。

最終重製清單有 666 個文字鍵、1330 WAV（含女性、四角色及第二章變體），共約 118.3 分鐘的全部素材，不是單次遊玩長度。`report-audio.json`：1330 段均有效、非靜音且符合產製 SHA256，最大峰值約 0.869；198 個第三章文字鍵均有四個不同模型；原文字鍵與六份保護檔案雜湊保持一致。`report-meta.json`：706 個原音檔 meta 逐檔相同。

首次實測發現從 LocalLow 執行的內附 worker 無法寫入較高完整性層級的 Temp，但能寫入遊戲自己的 LocalLow 資料區。語音請求與輸出已改放 `Application.persistentDataPath/ChineseVoiceRequests`，仍在結束／取消時刪除；沒有修改 Windows 安全設定。失敗的首次報告另存 `diagnostics/temp-permission-report.json`，不列為通過證據。

Unity 曾因 C 槽不足 1 GB 顯示對話框。只整理本次工作產生的檔案：舊 706 段錄音及 meta 已壓縮至 `D:/CodexBackups/wwww/chinese_natural_voice_20261104/before-audio.zip`，1412 個檔案逐一 SHA256 核對後才移除 C 槽的重複備份；另將本次 authoring env、MP3 中間檔及 portable test 移到該 D 槽目錄。專案與正式資源位置不變，細節見 `backup-archive.json`、`ARCHIVE-LOCATION.txt`；`before/` 仍有程式、設定及清單備份。重製可使用 D 槽 env 下的 `Scripts/python.exe` 執行 `Tools/natural-chinese-voice.py`。

## 最終 Editor 實測

`report-languages.json`：56 項通過，completed=true、errors=[]。實際載入語言選單、設定、第一章、第二章、第三章，測試原預設與字幕行為、第一章男女音檔與播放等待、第二章全部 59 中文及 28 個原有 clip、第三章嘴型與原族語風格；同一句依四個角色分別載入四份不同資源，角色快取不串音。

四角色未快取新句均實際連網產生並播放，LastProvider=online-neural；另測離線新句的本機備援、換句停止、啟動 worker 後立即取消。結束無遺留 worker，ChineseVoiceRequests 為空；原四個 PlayerPrefs 設定還原，測試自動停止，playing=false、dirty=false、compileFailed=false。這是固定測試輸入、暫停一般劇情後直接呼叫播放入口的配音驗證，未重新跑整章真人鍵鼠、實體 VR、獨立建置或實際 LLM 全流程。

收尾已將臨時驗證腳本及 meta 移至證據目錄 `verification-tools/`，清理後 Tundra build success（3.96 秒），Editor DLL 不含驗證類別，畫面亦確認 Play Mode 已停止。六份保護雜湊維持相同，最終 runtime 雜湊在 `final-state.json`，`git diff --check` 通過；未 commit／push。
