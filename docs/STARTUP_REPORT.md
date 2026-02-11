啟動報告 - Milk API Manager
===========================

日期: 2026-02-10

摘要:
- 已以 `docker-compose` 建構並啟動整套服務。
- 已確認並修正主機設定：`vm.max_map_count=262144`（已永久寫入 `/etc/sysctl.d/99-elasticsearch.conf`）。
- 啟動順序：先 `etcd`，確認 `etcd` 已就緒，接著啟動 `apisix` 與 `apisix-dashboard`，最後啟動其他服務（elasticsearch、grafana、kibana、logstash、prometheus、jaeger）。

當前狀態（摘錄）:
- `etcd`: Up, SERVING，leader 已選出。
- `apisix`: 已啟動（監聽 9080/9091/9443）。
- `apisix-dashboard`: 已啟動（監聽 9000），啟動期間曾因 `etcd` 尚未就緒出現 `connect: connection refused` 警告，但後續仍成功啟動。
- `elasticsearch`: 已啟動並變為 GREEN（先前有 vm.max_map_count 警告，已修正）。

已執行項目:
1. 檢索並觀察日誌（`apisix`、`apisix-dashboard`、`elasticsearch`）
2. 設定並永久保存 `vm.max_map_count=262144`
3. 逐步啟動服務（先 etcd -> apisix/apidash -> 其餘）
4. 執行 `scripts/sync-openapi-to-apisix.sh`（已觸發）

建議後續:
- 若要驗證完整流量，執行 API 呼叫測試（e.g., curl 到 `apisix` 端點）。
- 若需自動化啟動，可在 `docker-compose.yml` 優化啟動順序或加入 healthcheck 與 restart policy。

備註: 日誌顯示的 `connection refused` 多為 `etcd` 尚未完全就緒時產生，已透過逐步啟動降低該情況。

---

日期: 2026-02-11

今日工作摘要:
- 修正並測試 `docker-compose` 與 APISIX 配置（包含更換可用的 `etcd` 映像、移除不存在的自訂插件，並改為 traditional 模式以啟用 Admin API）。
- 啟動並驗證所有容器（APISIX、etcd、apisix-dashboard、prometheus、grafana、jaeger、elasticsearch、logstash、kibana）。
- 啟動並除錯後端服務：
	- 啟動 `MilkApiManager`（ASP.NET Core）於 `http://localhost:5001`（Development 模式以啟用 Swagger）。
	- 修正 `MilkAdminBlazor` 問題：`Pages/_Host.cshtml` 新增 `@page "/"`，並以 `http://localhost:5002` 啟動 Blazor UI。
- 啟動 Flask wrapper（`backend/app.py`）於 `http://localhost:5000`；執行測試發現 GET 端點正常，POST 寫入端點回傳 500，需要進一步 debug（已保留錯誤日誌與測試結果）。
- 進行 Git 清理與提交：新增 `.gitignore`（排除 `bin/`、`obj/`、IDE 產物等），從索引移除已追蹤的 build 檔並提交 `chore: add .gitignore and remove build artifacts from index`，再推送至 `origin/main`。

測試重點與待辦:
- Flask POST 500 錯誤：Enable Flask debug 或檢視應用日誌以定位例外堆疊並修復（下一步）。
- 建議把 `BACKEND_TEST_RESULTS.md`、`TEST_REPORT.md` 視為臨時輸出，若要保留於 repo 請告知；目前 `.gitignore` 已列入相關規則。

短期建議:
- 在 CI 或本機開發流程中避免將 build 產物加入版本控制；將 `.gitignore` 規則提供給團隊並在 repo policy 中強調。
- 建立針對寫入（POST/PUT/DELETE）端點的單元/整合測試，避免線上手動測試遺漏錯誤。

