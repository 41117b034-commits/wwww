# 第三章男聲風格銜接第一章（2026-11-04）

使用者要求第三章德固達雅賽德克語配音參考第一章背景族人男性的語調與聲音特色。這是配音風格調整，不是變更語別、字幕或劇情。

## 實際參考與調整

核對正式 `第一章新版警察.unity`：`maleReceiverThanksVoice` 指向 `Assets/音樂/喝.mp3`（GUID b62d479a9186a104eb4c4b42e54e2ffd）；`groomReplyVoice` 指向 `Assets/音樂/我們只是辦婚禮，沒有冒犯。.mp3`。第一章劇情對話是 2D 播放。原始錄音及場景不變。

Unity 解碼錄音後，以 50 ms 分析窗量測有聲段音高：背景男性短句約 148 Hz，新郎對話約 96 Hz，第三章「前面有巡警……」官方男聲約 156 Hz。這是短樣本的聲學估計，不能等同真人聽感或語言／族群判定；本次沒有可用的音訊聽辨工具。

以背景男性短句為主要參考：播放 pitch 0.95，約降 0.89 半音、播放時間增加 5.26%。保留官方合成語音的原發音與句內起伏，不將新郎的低頻估值直接用來大幅壓低所有角色。

新增 `Chapter3VoiceTone`，每句首次載入時進行 65 Hz 高通、220 Hz 低棚 +1.5 dB、3200 Hz 高棚 -2.5 dB，依有聲區塊做有限幅度的音量調整並柔化音量尖峰。維持 2D、音量 0.85，不加室內殘響。原 198 段官方 WAV 與翻譯清單不改寫；載入後的處理結果快取重用，即時合成音檔也走同一處理流程。生成的 AudioClip 在物件銷毀時釋放。

`Chapter2SpeechFace.BeginRecordedLine` 的時間改依播放 pitch 換算，錄音嘴型以 AudioSource 實際是否播放為準，避免較慢的配音最後約 5% 嘴型提前閉合。文字驅動的演出保留原時間邏輯。

這不是聲音複製，也沒有把第一章短錄音上傳成聲紋模型。官方服務的自訂聲音功能需要音檔與相符逐字稿；本次未使用它。來源仍是原民會語發基金會官方德固達雅男聲合成，譯文維持待族語老師校對狀態。

## 驗證與試聽

證據：`_CodexBackups/chapter3_voice_style_20261104/`。

- `reference-analysis.json`：第一章三個參考錄音及第三章原樣本的聲學估計。
- `report-dsp.json`：直接編譯並執行正式 C# 音訊處理程式，198 段均有限、非靜音、尖峰低於 0.901；零輸入保持靜音，立體聲聲道狀態互不滲入。此層是在 Unity 外執行，不能當成 Editor Play Mode。
- `third-chapter-style-preview.wav`：實際正式處理程式輸出，包含 0.85 音量與 0.95 播放速率，方便和 `third-chapter-before.wav` 比較。約 7.77 秒，估計音高中位數 147.88 Hz。
- `compile/runtime.log`、`compile/editor.log`：使用 Unity 6000.0.58f1 的 Roslyn 編譯器與目前 Bee response file 編譯正式程式及驗證工具，exit=0；輸出改至本次證據目錄，沒有替換開啟中 Editor 的 DLL。這證明程式可編譯，不代表 Editor 已重新載入新版本。
- `report-style.json`：使用者還原 Unity 後，Editor 重新編譯／載入並於正式第三章執行，16 項 completed=true、errors=[]。固定句及即時官方合成均播放處理後音檔，中文保留，變速後播放時間完整，嘴型到句尾，清空停止、重播使用快取、198 段在 Editor 內處理後均有效。樣本實際播放約 7.76 秒（預期 7.77 秒）、嘴型峰值 0.9。另用持續聲驗證超過原始音檔時長後嘴型仍保持活動，專門重現變速後提早閉嘴的風險。即時官方回傳 `msange ta hini han ma, soopo ta mosa deno.`，不代表語言正確性認證。
- `edit-state.txt`：測試自動停止 Play Mode，playing=false、dirty=false、compileFailed=false。此為 Editor 驅動配音專項測試：暫停駐在所正常流程並指定測試句，沒有重跑整章。先前整章 152 項仍僅代表前一輪成果。
- 臨時 `Chapter3VoiceStyleVerify.cs` 與 meta 已移回 `verification-tools/`。透過編輯器 Refresh 完成清理編譯（Tundra 3.32 秒），Editor DLL 已無驗證類別，沒有留下自動執行指令。日後需重測時，複製回 `Assets/Editor`、Refresh 並等待 `ready.txt` 更新，寫入本目錄 `command.txt` 內容 `run`；完成後移出工具並 Refresh。

聲音相似程度與自然度仍需使用者試聽；族語發音／翻譯仍需老師校對。此次不宣稱真人全程操作、VR 或獨立建置驗證。
