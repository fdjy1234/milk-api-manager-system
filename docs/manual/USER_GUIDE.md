# 📖 Milk API Manager 使用操作手冊

歡迎使用 Milk API Manager！本手冊旨在幫助管理員與開發者快速熟悉系統功能。

---

## 1. 🚀 快速開始 (管理員)

### 如何發布一個新的 API？
1.  **代碼先行**：在後端 `MilkApiManager` 中撰寫標準的 Controller 代碼。
2.  **自動同步**：重啟服務後，系統會自動將路由同步至 APISIX 網關。
3.  **驗證**：進入 **"Developer Hub"** -> **"API Explorer"**，確認服務已出現在列表中。

### 如何開啟隱私防護 (PII Masking)？
1.  進入 **"PII Protection"** 頁面。
2.  點擊 **"Add New Rule"**。
3.  輸入目標 **Route ID** 與 **欄位路徑** (如 `email`)。
4.  輸入 Regex 規則 (預設 `.*` 為全遮蔽)。
5.  儲存後，網關會即時生效，所有回傳數據中的該欄位都會變成 `***`。

---

## 2. 👩‍💻 開發者自助服務 (開發者)

### 如何獲取 API 訪問權限？
1.  進入 **"Developer Hub"**。
2.  在 **"API Explorer"** 標籤頁瀏覽可用的服務，並點擊 **"Documentation"** 查看詳細規格。
3.  切換至 **"Request Access"** 標籤頁。
4.  填寫您的專案名稱、聯絡信箱，並選擇 **Performance Tier** (Gold/Silver/Free)。
5.  提交申請後，請等待管理員審核。

### 如何測試我的 API？
*   在 **"API Explorer"** 中點擊 **"Run Tests"**。
*   您可以直接點擊 **"Run"** 按鈕發起一次真實的 API 請求，系統會回報當前的延遲與狀態。

---

## 3. 🚨 安全與維運 (管理員/維運)

### 處理自動封鎖 (Auto-Blocking)
*   當系統偵測到某 IP 異常掃描時，會自動將其加入 **"IP Blacklist"** 並發送 Webhook 警報。
*   若確認為誤判，您可以前往 **"IP Blacklist"** 頁面手動移除該 IP。

### 效能監控
*   進入 **"Traffic Intelligence Center"**。
*   開啟 **"Auto-Refresh"**，您可以看到即時的 P95 延遲趨勢。
*   若發現某 API 出現在 **"Performance Bottlenecks"** 列表，請聯繫對應的開發團隊優化代碼。

### 日誌審計與匯出
*   進入 **"Audit Logs"** 查看所有管理操作（如誰改了規則、誰審核了申請）。
*   點擊 **"Export CSV Report"** 下載報表，用於合規備查。

---

## 🛠️ 疑難排解 (Troubleshooting)

*   **API 回傳 404**：請檢查 `Route Sync Service` 日誌，確認路由是否已成功下發至 APISIX。
*   **Kibana 無數據**：請確認 `Logstash` 容器是否正常運行 (Port 8080/8081)。
*   **通知收不到**：請在 `appsettings.json` 或資料庫中確認 Webhook URL 配置是否正確。

---
*欲了解更多技術細節，請參考 [ARCHITECTURE.md](../../ARCHITECTURE.md)。*
