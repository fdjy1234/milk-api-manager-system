const fs = require('fs');
const path = require('path');

// 模擬 Elasticsearch Client
class MockEsClient {
  async search(query) {
    console.log('Executing ES Query:', JSON.stringify(query, null, 2));
    // 回傳假資料
    return {
      hits: {
        hits: [
          { _source: { timestamp: '2026-02-08T10:00:00Z', path: '/api/v1/user', status: 401, client_ip: '192.168.1.50' } },
          { _source: { timestamp: '2026-02-08T11:30:00Z', path: '/api/v1/admin', status: 403, client_ip: '10.0.0.5' } },
        ]
      }
    };
  }
}

async function generateReport() {
  const client = new MockEsClient();
  
  // 查詢過去 24 小時的異常 (Status >= 400)
  const query = {
    index: 'apisix-logs-*',
    body: {
      query: {
        bool: {
          filter: [
            { range: { '@timestamp': { gte: 'now-1d/d', lte: 'now/d' } } },
            { range: { status: { gte: 400 } } }
          ]
        }
      }
    }
  };

  const result = await client.search(query);
  const logs = result.hits.hits.map(h => h._source);

  // 產生 CSV
  const csvHeader = 'Timestamp,Path,Status,ClientIP\n';
  const csvRows = logs.map(l => `${l.timestamp},${l.path},${l.status},${l.client_ip}`).join('\n');
  
  const reportPath = path.join(__dirname, '../report_output.csv');
  fs.writeFileSync(reportPath, csvHeader + csvRows);
  
  console.log(`Compliance report generated: ${reportPath}`);
}

generateReport();
