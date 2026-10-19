# 第三章第七批：整章串接與驗證

版本提醒（2026-10-19）：本文件的 935 項報告與 `chapter3_batch7_20261016/build/Wushe.exe` 均為原兩路版本。後續新增達多攻入中央會場、三選一與 v2 結果格式的實作及驗證另見 [攻入學校紀錄](Chapter3-assault.md)；本次新增路線尚未重新產出 Windows 執行檔。

更新：2026-10-16。**第七批桌面整合、935 項 Editor 檢查與 Windows 執行檔產出已完成；獨立版啟動受工具存取限制，實體 VR 及全平台驗收仍待。** 第七批是 `Chapter3-batches.md` 原七批規劃的最後一批；不代表已完成正式第四章或所有發布條件。

## 範圍與固定流程

沿用 `Assets/Scenes/第三章.unity` 單一正式入口，串接第二章兩種立場、第三章開場、單一武器、山路交戰、學校分工、救援分支、林間結尾與完成結果。第二章支持／拒絕都接同一第三章；第三章仍從馬赫坡駐在所開始。原部落與山林保留場景不加入正式入口。

本輪以 `PROJECT_CONTEXT.md`、七批計畫及目前程式為準，並參考 `Documentation/Story/賽德克事件劇情脈絡.txt` 的第二章與突襲、救援、撤退段落。舊劇本的章數、選項與救援方式不能覆蓋後續已確定的遊戲規則。人物尺寸、場景資產與最近恢復的 Editor Game 預覽比例不因整合而重新調整。

四個必要結果組合仍為巴萬左線＋救援、巴萬左線＋留下、比胡右線＋救援、比胡右線＋留下。救援必定生還，含玩家四人抵達；留下時由達多返回確認死亡，含玩家三人抵達。莫那稍後到場，不計入原小隊人數。第四章讀取介面保留，但本輪不製作正式第四章。

## 本輪程式變更

目前共十個程式檔：九個遊戲 runtime（含一個新增 partial）及一個 TMP 範例建置修正。Editor 935 項檢查的原始碼基準為 `tested-runtime-hashes.json`；之後僅將駐在所 `#else` Player 分支的局部變數 `i` 改名為 `labelIndex`，解決獨立版 CS0136 編譯衝突。此段不參與 Editor 編譯，故不重跑未受影響的整章測試；Player 建置版另記於 `build-runtime-hashes.json`。

| 檔案 | 本輪變更 |
| --- | --- |
| `Assets/Chapter2/Chapter2Controller.cs` | 第二章兩分支完成原演出、淡黑與結尾停留後交給共用接續入口；不再因舊 `stopEditorAfterEnding` 序列化值停止 Play。卸載時取消探索交談。 |
| `Assets/Chapter2/Chapter2Controller.Transition.cs`（新增，附 `.meta`） | 正式第二章結果保存、失敗提示與重試、禁止提早／重複轉場、非同步載入第三章；清理對話與音效、恢復時間倍率。提供無全域測試開關的保存 delegate overload，失敗會還原記憶體中的舊結果鍵。 |
| `Assets/Chapter3/Chapter3OpeningConversation.cs` | 開場面板 `Begin` 保留純暖機，仍中止舊回答；請求使用獨立歷史副本，只有實際顯示的回覆進入正式短上下文；重入清除診斷值，回覆期限處理無效數值。 |
| `Assets/Chapter3/Chapter3ReadinessConversation.cs` | 模型輸出與正式歷史分離，被拒絕的標籤／內容不進入下一題；保留實際展示的澄清或固定準備決定，重入清除診斷值並保留逾時與取消規則。 |
| `Assets/Chapter3/Chapter3EndingConversation.cs` | 結尾面板開啟時保留 outcome／清點期間已啟動的服務準備；回答中的請求仍會取消。人物事實、生死、任務與保存仍由固定程式決定。 |
| `Assets/Chapter3/Chapter3Ending.cs` | 保留明確呼叫 `Suspend()` 時的鏡頭、莫那父物件與結束卡還原；`OnDisable` 僅取消協程與交談，避免重玩／返回選單卸載場景時，在停用中的父物件內移動莫那而拋出錯誤。 |
| `Assets/Chapter3/Chapter3StationController.cs` | 準備確認或聊天面板開啟時，不再由 `LateUpdate` 重寫「只能持有一種武器」提示；`TakeWeapon` 拒絕重複領取時也不覆寫開啟中的聊天提示，避免底部文字與對話面板重疊。原武器排他規則不變。實際建置另修正 `RefuseDeparture()` 的 Player 專用局部變數命名衝突，僅改名，演出不變。 |
| `Assets/Chapter3/Chapter3Presentation.cs` | 進入 VR 前保存桌面 Canvas 的父物件、順序、render mode 與 RectTransform；退出 VR 恢復原版面，避免持續掛在頭部攝影機下。 |
| `Assets/Chapter3/Chapter3ResultStore.cs` | 新增無全域狀態的保存 delegate overload；保存 flush 失敗時還原原本存在／不存在的鍵。完整版本 1 JSON 的嚴格讀取是既有行為，本輪保留並列入回歸檢查。 |
| `Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextInfoDebugTool.cs` | `using UnityEditor` 改以 `UNITY_EDITOR` 包住，避免範例偵錯工具的 Editor 命名空間進入 Player 編譯。這項原始碼修正本身不等同建置成功。 |

## Unity 入口與操作

| 入口／操作 | 實際用途 |
| --- | --- |
| 章節選單的第三章 | 載入正式第三章場景，從清晨駐在所開始。 |
| 第二章正常完成支持或拒絕 | 保留原台詞、動作、鏡頭；最後淡出完成後，支持再停留原字卡 3 秒，拒絕再停留黑幕 0.8 秒，然後保存第二章並自動載入第三章。 |
| 第二章 P 快速會議 | 仍可選支持／拒絕並接第三章，但沒有完成白天路線，因此不覆寫正式第二章結果。 |
| F6／F7／F8 | 同一第三章內分別進駐在所／山路短交戰／學校運動會；F8 也可重進學校。 |
| 學校前空白鍵 | 直接進運動會；聊天面板開啟、文字輸入或準備拒絕已確定時不跳段。 |
| 學校內空白鍵 | 略過當句允許略過的字幕，不重置任務；進學校的同一幀不連帶略過首句。 |
| 重玩第三章 | 重新載入正式第三章場景，清空本輪流程與裝備狀態，保留上一筆已完成結果。 |
| 返回章節選擇 | 載入既有 `選擇章節ˊ` 場景，保留已完成結果。 |

第二章公開接續入口為 `ContinueToChapter3()`，僅 `Stage.Complete` 可使用。`TransitionInProgress` 防止重複載入；失敗時停留在黑底提示，按「重試並前往第三章」再嘗試。`HoldAfterEndingForValidation` 是非序列化的執行期驗證開關，正式預設為 false；舊 `stopEditorAfterEnding` 欄位僅保留舊探針相容性。

第二章會議的字幕使用依字數計算的固定閱讀時間及 `Time.unscaledTime`，不是 `lineSeconds`。僅調高 `Time.timeScale` 不能縮短主要字幕等待；不得把用反射跳過會議的測試記成完整第二章演出。

同場景跳段由 Flow 在停用分區前明確呼叫 `Suspend()`，此時仍正常還原結尾暫借的莫那、鏡頭與 UI。重玩／返回選單則直接卸載整個場景，`Chapter3Ending.OnDisable` 只取消協程與交談，不在 Unity 拆除階層期間重新指定莫那的父物件。若只是暫時停用元件，暫借狀態保留到下一次明確的 `Suspend()`／`Begin()` 再安全還原。

## 重玩與存檔邊界

第二章鍵為 `WusheEvent.Chapter2.Result`，保留原 tree／meeting 選擇與完成資料格式；P 預覽、尚未完成或停用保存的驗證不覆寫它。第三章鍵為 `WusheEvent.Chapter3.Result`，仍只有莫那收尾與最後淡出完成後才提交版本 1 結果；LLM、任務中途、玩家倒下、重試、F8、重玩與返回選單均不得提前提交。

第三章版本 1 保存任務、救援選擇、單一武器、比胡生死、原小隊生還人數及角色的親歷／轉述知情來源。沿用既有 `Load()` 的完整 JSON 與 canonical 結果檢查，不接受遺漏、重複／多餘欄位、型別錯誤或矛盾結果；空白與欄位順序不影響有效性。這是本輪回歸項目，不是本輪新增的讀取格式。自由輸入、聊天紀錄不進結果鍵。

兩章保存都先記錄鍵原本是否存在與原字串，再寫入新結果並 flush。若 flush 拋出受處理的保存例外，會把 PlayerPrefs 的記憶體內容恢復原值；原本不存在就刪回，**rollback 不再呼叫 Save**。如此後續其他系統的 `PlayerPrefs.Save()` 不會意外寫入剛才失敗的新完成結果。第三章仍可由結束卡「重試保存」重試；第二章由接續按鈕重試。

注入 `Action persist` 拋出 `PlayerPrefsException` 可驗證這個邊界，無須更動磁碟權限或填滿磁碟。這是保存失敗控制路徑驗證，不是實際 OS 磁碟失效測試。所有驗證需備份並還原使用者原鍵。

## LLM 暖機、歷史與取消

繼續使用原 `Chapter2LocalDialogue` 及原服務設定，沒有在本輪換模型、修改憑證或啟用計費。開場／結尾提早呼叫 `PrepareService()`；真正有答案請求時仍套用原 15 秒期限與預寫備援，不保證每次即時生成成功。

`Begin` 僅保留尚無回答請求的純 `Prepare()`。共用傳輸的 `Busy` 在回答首次 yield 前已設為 true，包括等待服務準備的回答；因此舊回答即使正等暖機也會取消。對話自身的舊協程、版本與 IME 訂閱一併重設。F6／F7／F8、停用或離開時則全部取消，不保留暖機。

開場與準備確認把複製的短歷史交給傳輸，完成後只把最後實際展示的玩家話語／答覆加入正式歷史。逾時、拒絕格式及取消後的模型回覆不得汙染下一輪。章末原有固定事實回覆及獨立快照繼續保護生死、人數、分工與知情範圍。

桌面等待生成時沿用既有 `Chapter3DialogueFrameBudget`，結束、備援、重入與取消都恢復原 `targetFrameRate`／`vSyncCount`。VR 模式不降幀。受控暖機協程與離線逾時測試只能證明這些程式分支，不代表真正冷啟動模型或實際斷網測過。

## VR 版面與尚需另外完成的操作

本輪修復的是 Canvas 模式來回切換：VR 保留既有相機前方世界空間面板，退出後恢復原桌面父物件、幾何與顯示模式。直接呼叫 `ConfigureVR(true/false)` 的 Editor 檢查不等於戴頭盔、追蹤、舒適度或手把驗收，也不更動 Editor Game View 的正常等軸縮放。

現有 `Chapter3Player` 已讀取頭部位置／旋轉、左搖桿移動、右搖桿 30 度轉向及右扳機的單次 `ActionPressed`。以下仍是獨立 VR 輸入與實機工作，不能因 Canvas 檢查通過而標成完成：

| 項目 | 目前程式界線／待辦 |
| --- | --- |
| 武器攻擊、瞄準與裝填 | `Chapter3Weapons.Update()` 仍以滑鼠左右鍵與 R 為主；需追蹤控制器的持握、攻擊／射擊、瞄準與裝填映射及實機確認。 |
| 長按合作與救援 | 拆橫木、扶起比胡仍直接讀鍵盤 E 的 held 狀態；現有右扳機單次 `ActionPressed` 不足以完成長按流程。 |
| 武器、任務與救援選項 | 鍵盤 1／2 與 UI 按鈕已存在，尚需完整 XR 射線／手把點選與焦點流程驗證。 |
| 玩家倒下重試 | 山路、學校、救援重試仍讀 R；需手把對應與提示。 |
| 自由文字對話 | 需 VR 文字輸入、繁體中文／IME、送出及閱讀捲動操作；語音輸入另列工作。 |
| 結束卡與章節選單 | 需以頭戴與控制器實際選重玩、返回選單、重試保存及章節進出。 |
| 跳段與字幕略過 | F6／F7／F8／空白鍵為桌面製作入口；手把可用操作與呈現方式另行定義。 |
| 執行效能與舒適度 | 需真實頭戴幀率、畫面比例、字幕可讀性、轉向與扶行舒適度，以及目標平台建置實機測試。 |

## 本輪已完成的 Editor 驗證

證據目錄：`_CodexBackups/chapter3_batch7_20261016/`。以下十五份根目錄報告共 **935 項檢查**，均 `completed=true`、每項為 True、`errors=[]`。其中四份第二章入口／保存報告各記錄一則既有音源警告，合計四則；其餘十一份 `warnings=[]`。Windows 建置診斷另列下方，不合併成 Editor 測試零錯誤的宣稱。

| 本輪報告 | 檢查數 | 精確範圍與限制 |
| --- | --- | --- |
| `test-bawan-rescue-rifle.json` | 159 | 駐在所開場至左側突破、救援生還、莫那收尾與保存；直接重玩後回清晨、裝備重設、保留已完成結果。 |
| `test-bawan-leave-knife.json` | 145 | 駐在所開場至左側突破、留下死亡；達多實際回林路確認並返回、完整章末保存與直接重玩。 |
| `test-pihu-rescue-knife.json` | 178 | 駐在所開場至右側兩段掩護、救援生還、章末保存；實際返回章節選單再進第三章，舊結果保留。 |
| `test-pihu-leave-rifle.json` | 181 | 駐在所開場至右側兩段掩護、留下死亡、回報與保存；實際返回章節選單再進第三章，舊結果保留。 |
| `test-ending-lifecycle.json` | 63 | 配置莫那暫借／死亡鏡頭情境，經學校與駐在所重入確認父物件、局部位置／旋轉／比例、活動狀態、鏡頭與控制恢復；直接停用再明確 `Suspend()` 仍能還原，舊協程不復活。這是受控生命週期驗證，不是另外一輪整章。 |
| `test-entry-support.json` | 20 | 第二章真實會議 coroutine、UI 支持選擇、完整淡出與字卡停留後，自動載入第三章；正式結果、清晨入口與相機／Listener／EventSystem 唯一性。白天完成條件由測試注入。 |
| `test-entry-refuse.json` | 20 | 第二章真實會議 coroutine、UI 拒絕選擇、完整淡出後，自動載入第三章；正式結果、清晨入口與唯一性。白天完成條件由測試注入。 |
| `test-entry-preview-support.json` | 18 | 實際 P 預覽會議支持分支接第三章，保留舊第二章與第三章結果。本輪 P 預覽只測支持；不把它寫成兩個 P 分支都測過。 |
| `test-entry-save.json` | 9 | 注入完成前置條件與保存例外，驗證原本無鍵／已有鍵兩種 rollback、錯誤後可見重試按鈕，再透過公開按鈕正常保存並實際載入第三章。未誘發 OS 磁碟故障。 |
| `test-store.json` | 52 | 既有版本 1 格式回歸：四種 canonical 結果的寫入／讀回、矛盾知情／人數／分工及損壞或不完整資料拒收，無效保存不覆蓋舊值；原鍵備份並還原。這一組不遊玩章節。 |
| `test-lifecycle.json` | 24 | 直接切換 VR Canvas／恢復桌面；相機、AudioListener、EventSystem 唯一；第三章保存失敗的不存在／既有鍵 rollback；Combat／School／Station 分區切換及原結果還原。未戴頭盔、未誘發磁碟故障。 |
| `test-dialogue.json` | 35 | 開場／章末保留受控暖機；三處離線備援、正式歷史、取消與重入；明確準備回答、幀率恢復及結果不被改寫。沒有真實模型請求或服務設定變更，不能代替真實冷啟動與物理斷線驗證。 |
| `test-audio.json` | 11 | 隔離 AudioListener 取樣證實學校環境聲、玩家步槍與巴萬掩護槍聲有非零輸出。刀攻擊有執行但輸出為零；刀擊／裝填目前無專用音效觸發，不把它們列為聲音通過。詳見下方音訊界線。 |
| `test-live-llm.json` | 11 | 孤立章末交談元件使用 canonical 待完成結果；準備服務 3.83 秒，第一輪 11.09 秒取得 1 次符合限制的真正生成回覆，第二輪 15.02 秒到期後接 1 次預寫備援。兩輪後等待按鈕、callback 恰一次、幀率與舊結果保護通過。 |
| `test-cancel.json` | 9 | 孤立章末交談元件在非固定問題等待中按 F8，學校分區 `OnDisable` 取消請求並恢復控制／幀率；另等 16 秒，UI、callback、結尾與存檔均未被遲到回覆喚回。不是中斷完整 `Chapter3Ending` 演出。 |

分類合計為四種完整第三章組合 663、結尾生命週期 63、第二章入口／保存 67、格式保存 52、一般生命週期 24、受控對話 35、音效 11、真實 LLM 11、取消加測 9，共 935。初次發現莫那卸載錯誤的中間報告、`early-*` 報告與 `failure-*` 圖不計入或重複累加；四組完整流程以本表列出的最終根目錄報告為準。

四組第三章由駐在所開場、兩輪交談、選擇／領取一種武器、山路交戰自然接入學校，再完成任務、救援／留下、結尾與重新載入。操作由 CharacterController、Input System 事件、UI callback 及公開武器入口驅動；部分死亡與壓力由 `Damage`／`EscortHit` 注入，並以 `HoldFire` 隔離工作完成條件。章末使用受控逾時備援與固定事實回答，因此這四組不是四輪真實 LLM 品質或速度測試，也不是真人原生鍵鼠、實體 VR 或獨立 Player 遊玩。

第二章支持／拒絕正式保存測試注入的是白天完成前置條件，之後實跑原會議演出與 UI 選項。會議時鐘設為 3 倍，字幕仍依既有 unscaled 閱讀時間；不宣稱本輪重跑第二章片頭、自由探索、伐木與整章真人操作。Input System 測試使用臨時複製設定允許背景事件，報告記錄原設定已恢復，不把驅動事件當作原生 Game 鍵盤輸入。

`test-lifecycle` 24 項與 `test-dialogue` 35 項已在莫那卸載及聊天提示修正後重新執行通過；較早版本另存 `early-lifecycle.json`、`early-dialogue.json`，不重複計數。這十五份報告均對應 `tested-runtime-hashes.json` 的 Editor 行為。後來僅改動不參與 Editor 編譯的 Player `#else` 變數名稱，最終十檔改與 `build-runtime-hashes.json` 比對；不宣稱所有報告都在這項 Player 專用修正後重跑。

### 音效、真實服務與取消加測的界線

`test-audio` 從 Editor Play Mode 的隔離 AudioListener buffer 取樣，測得以下三種聲音有非零輸出：

| 聲音 | Peak | RMS | 觸發方式 |
| --- | --- | --- | --- |
| 學校環境聲 | 0.00710074 | 0.00144324 | 原學校循環 AudioSource。 |
| 玩家步槍 | 0.0569527 | 0.00738386 | 正常 `EnterCombat` 初始化檢查點後，透過既有 `StartFighting` 與公開 `Attack`。 |
| 巴萬合作掩護槍聲 | 0.205265 | 0.00984441 | 注入任務／路線前置條件，正常合作 `Update` 發出掩護射擊。 |

音訊測試繞過短交戰片頭淡入／台詞，部分敵人使用 `HoldFire`；不能據此驗收短交戰開場時序。實際刀攻擊亦執行，但取樣 peak／RMS 都為 0，現有刀揮擊、刀命中及裝填沒有專用音效觸發。以上不是原生聲卡錄音、真人聆聽、完整配樂、空間混音平衡或 VR 音效驗收。

`test-live-llm` 的服務準備為 3.83 秒；正常結尾會在 outcome／清點期間提早準備。兩輪非固定感受問題中，第一輪 11.09 秒真正生成，第二輪 15.02 秒到期後使用標示的預寫回覆。因此結果是 **1 次生成＋1 次備援**，不是兩次即時成功，也不能保證每次可在期限內回答；此處沒有證明真正離線模型冷啟動。

`test-live-llm` 與 `test-cancel` 都將獨立的 `Chapter3EndingConversation` 掛到 `school.gameObject`，提供 canonical 待完成結果；沒有重播學校任務、救援、清點與莫那演出。取消加測經 F8 實際停用學校分區並等待 16 秒，證明該元件的 `OnDisable` 與遲到回覆防護有效；完整結尾的角色／鏡頭清理由另一份 `test-ending-lifecycle` 與四組流程覆蓋，不能混稱本加測中斷過完整結尾演出。

### 第二章既有音源警告

四份入口／保存報告各有一則 `Can not play a disabled audio source`。既有第二章場景的 `Forest ambience` AudioSource 已序列化停用（`Assets/Scenes/第二章.unity:51478`），但原 `Chapter2Controller.Start()` 第 90 行及 `SetNight()` 第 458 行仍呼叫 `ambience.Play()`；啟動階段也可能在報告開始前出現同源警告。`fireAudio` 在場景中仍啟用，`Chapter2AudioManager` 沒有將兩者停用的程式，不能推稱是本次轉場或音樂管理器故意停用。

場景音源設定與上述兩個播放呼叫均早於第七批，歷史 `PROJECT_CONTEXT.md` 亦已有停用音源警告紀錄。本輪沒有更改這項既有設定，故兩種正式入口、P 預覽與保存重試均為無錯誤但非零警告驗證；四則是報告內計數，不是整個 Editor session 的警告總量。

### 已檢視的引擎渲染

主驗證者已實際檢視下列本輪畫面，範圍限於所列構圖與 UI；同目錄其他 PNG 的存在不自動代表已逐張驗收。

| 畫面 | 證據檔 |
| --- | --- |
| 領武器後準備確認／聊天面板 | `prelude-readiness.png` |
| 巴萬救援分支結束卡 | `test-bawan-rescue-rifle-chapter-complete.png` |
| 巴萬留下分支結束卡 | `test-bawan-leave-knife-chapter-complete.png` |
| 留下分支比胡最後倒臥 | `test-bawan-leave-knife-collapse-final.png` |
| 比胡救援分支返回章節選單 | `test-pihu-rescue-knife-chapter-menu.png` |
| 第二章支持後第三章清晨開場 | `test-entry-support-third-chapter.png` |
| VR Canvas 往返後桌面版面 | `lifecycle-desktop-restored.png` |

另已檢視 `test-live-llm-transcript.png` 的生成／備援紀錄與按鈕。最終直接操作 Unity，確認一般版面 0.45x、放大 Game 分頁 0.78x，第三章人物與完整畫面保持等軸比例；最後停在放大 Game 分頁、非 Play、第三章無未保存標記。這些是 Editor 視窗與引擎渲染證據，不能當作獨立 Player 鍵盤或頭戴舒適度測試。

## Windows 建置與啟動界線

實際使用 Unity 6000.0.58f1 的 StandaloneWindows64、Mono2x、Development／DetailedBuildReport，沿用原九個 enabled scenes 及順序。第二次 `BuildPipeline.BuildPlayer` 回報 `Succeeded`，耗時 1594.32 秒（約 26.6 分鐘），報告大小 3,700,519,799 bytes（約 3.45 GiB）。執行檔：`_CodexBackups/chapter3_batch7_20261016/build/Wushe.exe`。需保留整個 build 資料夾，不能只移動 exe；build 及原始 log 已由證據目錄內 `.gitignore` 排除追蹤。

**Succeeded 不等於零錯誤建置：同份報告有 5 則 error、40 則 warning。** 主要診斷包括既有 `第一章新版 1_找回備份` 的 missing prefab／script、章節選單與 XR simulation 設定的 missing script、舊 API／Unity Services 設定，以及舊特效 `Hidden/ColorCorrectionCurves`、`Hidden/Dof/DepthOfFieldHdr` 在 `STEREO_INSTANCING_ON` 變體缺少 `sampler_CameraDepthTexture`。本輪未擴大修復舊備份場景／第三方特效，也未刪除建置場景來隱藏診斷。詳見 `build-result.json` 及本機 `build-editor.log`；不能視為正式發布或 VR 材質驗收通過。

初次 Player 建置因駐在所 Player 專用局部變數同名而失敗（`diagnostic-build-player-only-name-conflict.json`）；改名後取得上述可執行檔。935 項 Editor 檢查對應該改名前的 `tested-runtime-hashes.json`，只有這個被 `#else` 隔離的變數名稱不同；實際建置與最後程式以 `build-runtime-hashes.json`、`runtime-final-check.json` 為準。

嘗試透過電腦操作工具啟動 `Wushe.exe` 被回覆 `Computer Use was not approved to use wushe`。使用者明確允許「啟動並測試」後重試，工具仍回覆同一限制。因此**沒有獨立版啟動、主選單、原生鍵盤或實際 Player 遊玩的通過證據**；不能把已產生 exe 或 Editor 重玩選單檢查當成獨立版驗收。

## 最終收尾

使用者原第二／第三章結果鍵已由每次測試備份與恢復；恢復函式逐鍵核對，十五份報告均完成清理。刪除臨時驗證工具前，`audit.txt` 記錄正式第三章 `dirty=false`、`playing=false`、`compileFailed=false`，missing scripts／MeshFilter mesh／SkinnedMesh 均 0。七個臨時 Editor 腳本及其 meta 已逐檔驗證備份相同後移出 Assets，封存在 `verification-tools/active-final/`；清理後 Editor DLL 重新編譯及 domain reload 成功，沒有留下測試 driver。`cleanup-editor.log` 包含歷史紀錄，不代表整個 session 沒有上述舊警告／建置診斷。

第一、第二、第三章場景、Build Settings、Flow 及 Game 預覽守衛雜湊維持修改前值，十個正式程式符合建置版雜湊。Unity 建置自行寫回的 PC Pipeline prefilter、URP runtime list、PlayerSettings preloadedAssets 已先備份至 `build-side-effects/` 再精確恢復原值；五份字型雖一度出現在 Git status，經 HEAD／工作檔原始位元組核對內容相同，未改寫。最後 `git diff --check` 通過，沒有 commit／push。

## 仍未驗證或另列工作

| 項目 | 截至本表的界線 |
| --- | --- |
| Windows Player 啟動及遊玩 | 已產出，工具在使用者明確允許後仍拒絕程式存取；尚未完成原生操作測試。既有 5 則 error／40 則 warning 待依發布範圍另行處理。 |
| 完整聲音體驗 | 已量測上述三個 cue；刀擊／裝填專用音效、真人聆聽、原生裝置錄音與完整混音尚未完成或驗證。 |
| 真實離線模型暖機、實體 VR 與真人整章操作 | 尚未建立本輪通過證據；沿用前述 VR 操作缺口與驗證界線。 |

第七批桌面整合工作已完成；**不將上述受限或另列項目標示為全部驗收結案**。
