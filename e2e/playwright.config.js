const { defineConfig } = require('@playwright/test');

const baseURL = process.env.BASE_URL || 'http://localhost:55894';

module.exports = defineConfig({
  testDir: './tests',
  timeout: 30000,
  retries: 0,
  use: {
    baseURL,
    headless: true,
    viewport: { width: 1400, height: 900 },
    screenshot: 'off'
  },
  reporter: [
    ['list'],
    ['html', { outputFolder: 'test-report', open: 'never' }]
  ],
  outputDir: 'test-results',
  workers: 1
});
